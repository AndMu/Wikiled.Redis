using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

        private ConnectionMultiplexer connection;

        private bool enabledNotifications;

        public RedisMultiplexer(ILogger<RedisMultiplexer> log, IRedisConfiguration configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
        }

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
            if (connection == null)
            {
                return;
            }

            connection.ConnectionFailed -= OnConnectionFailed;
            connection.ConnectionRestored -= OnConnectionRestored;
            connection.ErrorMessage -= OnErrorMessage;
            connection.InternalError -= OnInternalError;
            connection.Dispose();
            connection.Close();
            connection = null;
        }

        public void Configure(string key, string value)
        {
            log.LogWarning("Configure");
            CheckConnection();
            foreach (var server in GetServers())
            {
                server.ConfigSet(key, value);
            }
        }

        public Task DeleteKeys(string pattern)
        {
            log.LogWarning("DeleteKeys: <{0}>", pattern);
            int total = 0;
            var tasks = new List<Task>();
            foreach (var key in GetKeys(pattern))
            {
                total++;
                tasks.Add(Database.KeyDeleteAsync(key));
            }

            log.LogWarning("Deleted Keys: <{0}> - {1} keys", pattern, total);
            return Task.WhenAll(tasks);
        }

        public void Dispose()
        {
            var current = connection;
            Close();
            current?.Dispose();
        }

        public void Flush()
        {
            log.LogWarning("Flush");
            CheckConnection();
            foreach (var server in GetServers())
            {
                server.FlushAllDatabases();
            }
        }

        public void Shutdown()
        {
            log.LogWarning("Shutdown");
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
            log.LogWarning("GetKeys: <{0}>", pattern);
            return GetServers().SelectMany(server => server.Keys(pattern: pattern));
        }

        public ISubscriber GetSubscriber()
        {
            CheckConnection();
            return connection.GetSubscriber();
        }

        public void Open()
        {
            if (connection != null)
            {
                log.LogInformation("Connection is already open");
                return;
            }

            try
            {

                log.LogDebug("Opening...");
                var options = Configuration.GetOptions();
                foreach (var endpoint in options.EndPoints)
                {
                    log.LogInformation("Host: {0}", endpoint);
                }

                log.LogInformation(
                    "Other configuration - KeepAlive:[{0}] ConnectTimeout:[{1}] SyncTimeout:[{2}] ServiceName:[{3}] AllowAdmin:[{4}]",
                    Configuration.KeepAlive,
                    Configuration.ConnectTimeout,
                    Configuration.SyncTimeout,
                    Configuration.ServiceName,
                    Configuration.AllowAdmin);

                connection = ConnectionMultiplexer.Connect(options);
                Database = GetDatabase(connection);
                connection.ConnectionFailed += OnConnectionFailed;
                connection.ConnectionRestored += OnConnectionRestored;
                connection.ErrorMessage += OnErrorMessage;
                connection.InternalError += OnInternalError;
            }
            catch
            {
                connection = null;
                throw;
            }
        }

        public void SetupSlave(EndPoint master)
        {
            foreach (var server in GetServers())
            {
                server.SlaveOf(master);
            }
        }

        public ISubscriber SubscribeKeyEvents(string key, Action<KeyspaceEvent> action)
        {
            if (!enabledNotifications)
            {
                Configure("notify-keyspace-events", "KEA");
                enabledNotifications = true;
            }

            // in current implementation on DB 0 is supported. 
            var eventName = $"__keyspace@0__:{key}";
            ISubscriber subscriber = connection.GetSubscriber();
            var redisChannel = new RedisChannel(eventName, RedisChannel.PatternMode.Auto);
            subscriber.Subscribe(redisChannel, (channel, value) => action(new KeyspaceEvent(key, channel, value)));
            return subscriber;
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
            log.LogInformation(
                "Connection Restored: EndPoint='{0}'.",
                eventArgs.EndPoint);
        }

        private void OnConnectionFailed(object sender, ConnectionFailedEventArgs eventArgs)
        {
            log.LogError(
                "Connection Failed: EndPoint='{0}', FailureType='{1}', Exception='{2}'.",
                eventArgs.EndPoint,
                eventArgs.FailureType,
                eventArgs.Exception);
        }

        private IDatabase GetDatabase(ConnectionMultiplexer multiplexer)
        {
            foreach (var endPoint in multiplexer.GetEndPoints())
            {
                var server = multiplexer.GetServer(endPoint);
                if (server.ServerType == ServerType.Sentinel)
                {
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
                    log.LogInformation("Found a master: {0}:{1} ({2})", name, host, port);
                    server = multiplexer.GetServer(endPoint);
                }

                log.LogInformation("Looking for a database: {0}", endPoint);
                if (!server.IsSlave)
                {
                    IDatabase database = server.Multiplexer.GetDatabase();
                    return database;
                }
            }

            throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Failed to find MASTER server");
        }
    }
}
