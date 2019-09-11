using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Xml.Serialization;

namespace Wikiled.Redis.Config
{
    [XmlRoot("RedisConfig")]
    public class RedisConfiguration : IRedisConfiguration
    {
        public RedisConfiguration()
        {
            KeepAlive = 60;
            ConnectTimeout = 5000;
            SyncTimeout = 5000;
            ResponseTimeout = 10000;
            ServiceName = "Wikiled";
            AllowAdmin = true;
        }

        public bool AbortOnConnectFail { get; set; }

        public bool AllowAdmin { get; set; }

        public int ConnectRetry { get; set; }

        public int ConnectTimeout { get; set; }

        [XmlArray("Endpoints")]
        [XmlArrayItem("Endpoint")]
        public RedisEndpoint[] Endpoints { get; set; }

        public int KeepAlive { get; set; }

        public int ResponseTimeout { get; set; }

        public string ServiceName { get; set; }

        public string Password { get; set; }

        public int SyncTimeout { get; set; }

        public int WriteBuffer { get; set; }

        public RedisConfiguration(DnsEndPoint endPoint)
            : this()
        {
            if (endPoint == null)
            {
                throw new ArgumentNullException(nameof(endPoint));
            }

            Endpoints = new[] { new RedisEndpoint { Host = endPoint.Host, Port = endPoint.Port } };
        }

        public RedisConfiguration(string host, int? port = null)
            : this()
        {
            if (string.IsNullOrEmpty(host))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(host));
            }

            Endpoints = new[] { new RedisEndpoint { Host = host } };
            if (port.HasValue)
            {
                Endpoints[0].Port = port.Value;
            }
        }

        protected RedisConfiguration(RedisConfiguration redisSettings)
        {
            if (redisSettings == null)
            {
                throw new ArgumentNullException(nameof(redisSettings));
            }

            AbortOnConnectFail = redisSettings.AbortOnConnectFail;
            ConnectRetry = redisSettings.ConnectRetry;
            AllowAdmin = redisSettings.AllowAdmin;
            ConnectRetry = redisSettings.ConnectRetry;
            ConnectTimeout = redisSettings.ConnectTimeout;
            KeepAlive = redisSettings.KeepAlive;
            ServiceName = redisSettings.ServiceName;
            WriteBuffer = redisSettings.WriteBuffer;
            SyncTimeout = redisSettings.SyncTimeout;
            Password = redisSettings.Password;
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

      

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append($"[{ServiceName}] ");
            foreach (var redisEndpoint in Endpoints)
            {
                builder.Append($"[{redisEndpoint.Host}:{redisEndpoint.Port}]");
            }
            
            return builder.ToString();
        }

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
                KeepAlive = KeepAlive,              // 60 sec to ensure connection is alive
                ConnectTimeout = ConnectTimeout,    // 5 sec
                SyncTimeout = SyncTimeout,          // 5 sec
                ServiceName = ServiceName,          // sentinel service name
                AllowAdmin = AllowAdmin,
                AbortOnConnectFail = false
            };

            if (!string.IsNullOrEmpty(Password))
            {
                config.Password = Password;
            }

            foreach (var endpoint in Endpoints)
            {
                config.EndPoints.Add(endpoint.Host, endpoint.Port);
            }

            return config;
        }
    }
}
