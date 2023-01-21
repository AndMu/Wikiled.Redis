using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Wikiled.Redis.Config;
using Wikiled.Redis.Information;
using Wikiled.Redis.Serialization.Subscription;

namespace Wikiled.Redis.Logic
{
    public class RedisMultiplexer : IRedisMultiplexer
    {
        private readonly ILogger<RedisMultiplexer> log;

        private IConnectionMultiplexer connection;

        private IDisposable reconnection;

        private readonly Func<ConfigurationOptions, Task<IConnectionMultiplexer>> multiplexerFactory;

        public RedisMultiplexer(ILogger<RedisMultiplexer> log,
                                IRedisConfiguration configuration,
                                Func<ConfigurationOptions, Task<IConnectionMultiplexer>> multiplexerFactory)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.multiplexerFactory = multiplexerFactory ?? throw new ArgumentNullException(nameof(multiplexerFactory));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public bool UsingSentinel { get; private set; }

        public IRedisConfiguration Configuration { get; }

        public bool IsActive => connection != null;

        public IDatabase Database { get; private set; }

        public void CheckConnection()
        {
            if (connection == null)
            {
                throw new InvalidOperationException("Connection to redis wasn't established");
            }
        }

        public void Close()
        {
            log.LogDebug("Close");
            if (connection == null)
            {
                return;
            }

            try
            {
                connection.ConnectionFailed -= OnConnectionFailed;
                connection.ConnectionRestored -= OnConnectionRestored;
                connection.ErrorMessage -= OnErrorMessage;
                connection.InternalError -= OnInternalError;

                reconnection?.Dispose();
                reconnection = null;

                connection.Dispose();
                connection.Close();
                connection = null;
            }
            catch (Exception e)
            {
                log.LogError(e, "Close failure");
            }
        }

        public void Configure(string key, string value)
        {
            log.LogInformation("Configure");
            CheckConnection();
            foreach (var server in GetServers())
            {
                server.ConfigSet(key, value);
            }
        }

        public Task DeleteKeys(string pattern)
        {
            log.LogInformation("DeleteKeys: <{0}>", pattern);
            int total = 0;
            var tasks = new List<Task>();
            foreach (var key in GetKeys(pattern))
            {
                total++;
                tasks.Add(Database.KeyDeleteAsync(key));
            }

            log.LogInformation("Deleted Keys: <{0}> - {1} keys", pattern, total);
            return Task.WhenAll(tasks);
        }

        public void Dispose()
        {
            log.LogDebug("Dispose");
            var current = connection;
            Close();
            current?.Dispose();
        }

        public void Flush()
        {
            log.LogInformation("Flush");
            CheckConnection();
            foreach (var server in GetServers())
            {
                server.FlushAllDatabases();
            }
        }

        public void Shutdown()
        {
            log.LogDebug("Shutdown");
            CheckConnection();
            foreach (var server in GetServers())
            {
                server.Shutdown();
            }
        }

        public IEnumerable<IServerInformation> GetInfo(string section = null)
        {
            return GetServers().Select(server => new ServerInformation(server, server.Info(section)));
        }

        public IEnumerable<RedisKey> GetKeys(string pattern)
        {
            log.LogInformation("GetKeys: <{0}>", pattern);
            return GetServers().SelectMany(server => server.Keys(pattern: pattern));
        }

        public ISubscriber GetSubscriber()
        {
            CheckConnection();
            return connection.GetSubscriber();
        }

        public async Task Open()
        {
            if (connection != null)
            {
                log.LogDebug("Connection is already open");
                return;
            }

            try
            {
                log.LogDebug("Opening...");
                Database = await ResolveDatabase().ConfigureAwait(false);
            }
            catch
            {
                connection = null;
                throw;
            }
        }

        private async Task<IDatabase> ResolveDatabase()
        {
            var options = Configuration.GetOptions();

            foreach (var endpoint in options.EndPoints)
            {
                log.LogDebug("Host: {0}", endpoint);
            }

            log.LogDebug(
                "Other configuration - KeepAlive:[{0}] ConnectTimeout:[{1}] SyncTimeout:[{2}] ServiceName:[{3}] AllowAdmin:[{4}]",
                Configuration.KeepAlive,
                Configuration.ConnectTimeout,
                Configuration.SyncTimeout,
                Configuration.ServiceName,
                Configuration.AllowAdmin);

            connection = await multiplexerFactory(options).ConfigureAwait(false);
            connection.ConnectionFailed += OnConnectionFailed;
            connection.ConnectionRestored += OnConnectionRestored;
            connection.ErrorMessage += OnErrorMessage;
            connection.InternalError += OnInternalError;

            var database = await GetDatabaseFromMultiplexer(connection).ConfigureAwait(false);

            for (int i = 0; i < 5; i++)
            {
                if (connection.IsConnecting)
                {
                    log.LogInformation("Waiting to connect...");
                    await Task.Delay(500).ConfigureAwait(false);
                }
                else
                {
                    break;
                }
            }

            return database;
        }

        public void SetupSlave(EndPoint master)
        {
            foreach (var server in GetServers())
            {
                server.SlaveOf(master);
            }
        }

        public void EnableNotifications()
        {
            Configure("notify-keyspace-events", "KEA");
        }

        public ISubscriber SubscribeKeyEvents(string key, Action<KeyspaceEvent> action)
        {
            var eventName = $"__key*:{key}";
            ISubscriber subscriber = connection.GetSubscriber();
            var redisChannel = new RedisChannel(eventName, RedisChannel.PatternMode.Auto);
            subscriber.Subscribe(redisChannel, (channel, value) => action(new KeyspaceEvent(key, channel, value)));
            return subscriber;
        }

        public Task<ChannelMessageQueue> SubscribeKeyEvents(string key)
        {
            var eventName = $"__key*:{key}";
            ISubscriber subscriber = connection.GetSubscriber();
            var redisChannel = new RedisChannel(eventName, RedisChannel.PatternMode.Auto);
            return subscriber.SubscribeAsync(redisChannel);
        }

        public IEnumerable<IServer> GetServers()
        {
            return Configuration.Endpoints.Select(endpoint => connection.GetServer(endpoint.Host, endpoint.Port));
        }

        private void OnInternalError(object sender, InternalErrorEventArgs eventArgs)
        {
            log.LogError(
                "Redis Internal Error: EndPoint='{0}', Origin='{1}', Exception='{2}'",
                eventArgs.EndPoint,
                eventArgs.Origin,
                eventArgs.Exception);
        }

        private void OnErrorMessage(object sender, RedisErrorEventArgs eventArgs)
        {
            log.LogError("Redis Error Message: EndPoint='{0}', Message='{1}'.", eventArgs.EndPoint, eventArgs.Message);
        }

        private void OnConnectionRestored(object sender, ConnectionFailedEventArgs eventArgs)
        {
            reconnection?.Dispose();
            reconnection = null;

            log.LogInformation(
                "Connection Restored: EndPoint='{0}'.",
                eventArgs.EndPoint);
        }

        private void OnConnectionFailed(object sender, ConnectionFailedEventArgs eventArgs)
        {
            if (reconnection == null)
            {
                reconnection = Observable.Timer(TimeSpan.FromMinutes(1))
                                         .Select(item =>
                                         {
                                             // this required to use sentinel based failover
                                             log.LogWarning("Stale connection detected. Reconnecting...");
                                             Close();

                                             return Open();
                                         })
                                         .Subscribe(item => { });
            }

            log.LogError(
                "Connection Failed: EndPoint='{0}', FailureType='{1}', Exception='{2}'.",
                eventArgs.EndPoint,
                eventArgs.FailureType,
                eventArgs.Exception);
        }

        private Task<IDatabase> GetDatabaseFromMultiplexer(IConnectionMultiplexer multiplexer)
        {
            foreach (var endPoint in multiplexer.GetEndPoints())
            {
                var server = multiplexer.GetServer(endPoint);
                if (server.ServerType == ServerType.Sentinel)
                {
                    if (UsingSentinel)
                    {
                        throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Circular sentinel dependency detected");
                    }

                    UsingSentinel = true;
                    log.LogInformation("The server is sentinel. Querying its masters");
                    var master = server.SentinelMasters().FirstOrDefault();
                    if (master == null)
                    {
                        throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Failed to find MASTERS");
                    }

                    var settings = master.ToDictionary();
                    var name = settings["name"];
                    var host = settings["ip"];
                    var port = settings["port"];
                    Close();

                    log.LogInformation("Switching to the master: {0}:{1} ({2})", name, host, port);
                    Configuration.Endpoints = new[]
                    {
                        new RedisEndpoint
                        {
                            Host = host,
                            Port = int.Parse(port)
                        }
                    };

                    return ResolveDatabase();
                }

                log.LogInformation("Looking for database: {0} [{1}]", server.EndPoint, server.ServerType);
                if (!server.IsSlave)
                {
                    IDatabase database = server.Multiplexer.GetDatabase();
                    return Task.FromResult(database);
                }
            }

            throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Failed to find MASTER server");
        }
    }
}
