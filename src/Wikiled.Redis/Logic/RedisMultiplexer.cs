using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NLog;
using StackExchange.Redis;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Redis.Config;
using Wikiled.Redis.Information;
using Wikiled.Redis.Serialization.Subscription;

namespace Wikiled.Redis.Logic
{
    public class RedisMultiplexer : IRedisMultiplexer
    {
        private readonly RedisConfiguration configuration;

        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private ConnectionMultiplexer connection;

        private bool enabledNotifications;

        public RedisMultiplexer(RedisConfiguration configuration)
        {
            Guard.NotNull(() => configuration, configuration);
            this.configuration = configuration;
        }

        public IRedisConfiguration Configuration => configuration;

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
            if (connection != null)
            {
                connection.Close();
                connection = null;
            }
        }

        public void Configure(string key, string value)
        {
            log.Warn("Configure");
            CheckConnection();
            foreach (var server in GetServers())
            {
                server.ConfigSet(key, value);
            }
        }

        public Task DeleteKeys(string pattern)
        {
            log.Warn("DeleteKeys: <{0}>", pattern);
            int total = 0;
            List<Task> tasks = new List<Task>();
            foreach (var key in GetKeys(pattern))
            {
                total++;
                tasks.Add(Database.KeyDeleteAsync(key));
            }

            log.Warn("Deleted Keys: <{0}> - {1} keys", pattern, total);
            return Task.WhenAll(tasks);
        }

        public void Dispose()
        {
            if (connection != null)
            {
                connection.ConnectionFailed -= OnConnectionFailed;
                connection.ConnectionRestored -= OnConnectionRestored;
                connection.ErrorMessage -= OnErrorMessage;
                connection.InternalError -= OnInternalError;
                connection.Dispose();
            }
        }

        public void Flush()
        {
            log.Warn("Flush");
            CheckConnection();
            foreach (var server in GetServers())
            {
                server.FlushAllDatabases(CommandFlags.HighPriority);
            }
        }

        public IEnumerable<IServerInformation> GetInfo(string section = null)
        {
            return GetServers().Select(server => new ServerInformation(server, server.Info(section)));
        }

        public IEnumerable<RedisKey> GetKeys(string pattern)
        {
            log.Warn("GetKeys: <{0}>", pattern);
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
                log.Info("Connection is already open");
                return;
            }

            log.Debug("Openning...");
            connection = ConnectionMultiplexer.Connect(Configuration.GetOptions());
            connection.PreserveAsyncOrder = false;
            Database = connection.GetDatabase();
            connection.ConnectionFailed += OnConnectionFailed;
            connection.ConnectionRestored += OnConnectionRestored;
            connection.ErrorMessage += OnErrorMessage;
            connection.InternalError += OnInternalError;
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
            RedisChannel redisChannel = new RedisChannel(eventName, RedisChannel.PatternMode.Auto);
            subscriber.Subscribe(redisChannel, (channel, value) => action(new KeyspaceEvent(key, channel, value)));
            return subscriber;
        }

        public IEnumerable<IServer> GetServers()
        {
            return configuration.Endpoints.Select(endpoint => connection.GetServer(endpoint.Host, endpoint.Port));
        }

        private void OnInternalError(object sender, InternalErrorEventArgs eventArgs)
        {
            log.Error(
                "Redis Internal Error: EndPoint='{0}', Origin='{1}', Exception='{2}'",
                eventArgs.EndPoint,
                eventArgs.Origin,
                eventArgs.Exception);
        }

        private void OnErrorMessage(object sender, RedisErrorEventArgs eventArgs)
        {
            log.Error("Redis Error Message: EndPoint='{0}', Message='{1}'.", eventArgs.EndPoint, eventArgs.Message);
        }

        private void OnConnectionRestored(object sender, ConnectionFailedEventArgs eventArgs)
        {
            log.Info(
                "Connection Restored: EndPoint='{0}'.",
                eventArgs.EndPoint);
        }

        private void OnConnectionFailed(object sender, ConnectionFailedEventArgs eventArgs)
        {
            log.Error(
                "Connection Failed: EndPoint='{0}', FailureType='{1}', Exception='{2}'.",
                eventArgs.EndPoint,
                eventArgs.FailureType,
                eventArgs.Exception);
        }
    }
}
