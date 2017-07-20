using System.Collections.Generic;
using System.Xml.Serialization;
using NLog;
using StackExchange.Redis;
using Wikiled.Core.Utility.Arguments;

namespace Wikiled.Redis.Config
{
    [XmlRoot("RedisConfig")]
    public class RedisConfiguration : IRedisConfiguration
    {
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        public RedisConfiguration(string name)
        {
            KeepAlive = 60;
            ConnectTimeout = 5000;
            SyncTimeout = 5000;
            ResponseTimeout = 10000;
            ServiceName = name;
            AllowAdmin = true;
        }

        public RedisConfiguration(string name, string host, int? port = null)
            : this(name)
        {
            Guard.NotNullOrEmpty(() => host, host);
            Endpoints = new[] {new RedisEndpoint {Host = host}};
            if (port.HasValue)
            {
                Endpoints[0].Port = port.Value;
            }
        }

        protected RedisConfiguration(RedisConfiguration redisSettings)
        {
            Guard.NotNull(() => redisSettings, redisSettings);
            PoolConnection = redisSettings.PoolConnection;
            AbortOnConnectFail = redisSettings.AbortOnConnectFail;
            ConnectRetry = redisSettings.ConnectRetry;
            AllowAdmin = redisSettings.AllowAdmin;
            ConnectRetry = redisSettings.ConnectRetry;
            ConnectTimeout = redisSettings.ConnectTimeout;
            KeepAlive = redisSettings.KeepAlive;
            ServiceName = redisSettings.ServiceName;
            WriteBuffer = redisSettings.WriteBuffer;
            SyncTimeout = redisSettings.SyncTimeout;
            if (redisSettings.Endpoints != null)
            {
                Endpoints = new RedisEndpoint[redisSettings.Endpoints.Length];
                for (var i = 0; i < Endpoints.Length; i++)
                {
                    Endpoints[i] = new RedisEndpoint();
                    Endpoints[i].Host = redisSettings.Endpoints[i].Host;
                    Endpoints[i].Port = redisSettings.Endpoints[i].Port;
                }
            }
        }

        public bool AbortOnConnectFail { get; set; }

        public bool AllowAdmin { get; set; }

        public int ConnectRetry { get; set; }

        public int ConnectTimeout { get; set; }

        [XmlArray("Endpoints")]
        [XmlArrayItem("Endpoint")]
        public RedisEndpoint[] Endpoints { get; set; }

        public int KeepAlive { get; set; }

        public bool PoolConnection { get; set; }

        public int ResponseTimeout { get; set; }

        public string ServiceName { get; set; }

        public int SyncTimeout { get; set; }

        public int WriteBuffer { get; set; }

        public ConfigurationOptions GetOptions()
        {
            var config = new ConfigurationOptions
                         {
                             CommandMap = CommandMap.Create(
                                 new HashSet<string>
                                 {
                                     // EXCLUDE a few commands (to work with data-flow-related mode only)
                                     "CLUSTER",
                                     "PING",
                                     "ECHO",
                                     "CLIENT"
                                 },
                                 false),
                             KeepAlive = KeepAlive, // 60 sec to ensure connection is alive
                             ConnectTimeout = ConnectTimeout, // 5 sec
                             SyncTimeout = SyncTimeout, // 5 sec
                             ServiceName = ServiceName, // sentinel service name
                             AllowAdmin = AllowAdmin,
                             ResponseTimeout = ResponseTimeout
                         };

            foreach (var endpoint in Endpoints)
            {
                logger.Info("Configure Host: {0}", endpoint);
                config.EndPoints.Add(endpoint.Host, endpoint.Port);
            }

            logger.Info(
                "Other configuration - KeepAlive:[{0}] ConnectTimeout:[{1}] SyncTimeout:[{2}] ServiceName:[{3}] AllowAdmin:[{4}]",
                config.KeepAlive,
                config.ConnectTimeout,
                config.SyncTimeout,
                config.ServiceName,
                config.AllowAdmin);
            return config;
        }
    }
}
