using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Wikiled.Common.Extensions;
using Wikiled.Common.Logging;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Config;

namespace Wikiled.Redis.Logic.Pool
{
    public class RedisLinksPool : IRedisLinksPool
    {
        private static readonly ILogger logger = ApplicationLogging.CreateLogger<RedisLinksPool>();

        private readonly RedisConfiguration[] configurations;

        private Dictionary<string, RedisLink> links;

        private bool isDisposed;

        public RedisLinksPool(RedisConfiguration[] configurations)
        {
            this.configurations = configurations ?? throw new ArgumentNullException(nameof(configurations));
            State = ChannelState.New;
            logger.LogDebug("Adding {0} services", configurations.Length);
        }

        public string Name { get; } = "Pool";

        public ChannelState State { get; private set; }

        public void Close()
        {
            State = ChannelState.Closing;
            if (links != null)
            {
                foreach (var redisLink in links)
                {
                    redisLink.Value.Open();
                }
            }

            State = ChannelState.Closed;
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            if (links != null)
            {
                foreach (var redisLink in links)
                {
                    redisLink.Value.Dispose();
                }
            }
        }

        public void Open()
        {
            State = ChannelState.Opening;
            links = configurations.Select(item => new RedisLink(item.ServiceName, new RedisMultiplexer(item))).ToDictionary(item => item.Multiplexer.Configuration.ServiceName, link => link);
            foreach (var redisLink in links)
            {
                logger.LogDebug("Opening {0}", redisLink.Value.Multiplexer.Configuration);
                redisLink.Value.Open();
            }

            State = ChannelState.Open;
        }

        public IRedisLink GetKey(string key)
        {
            return links?.GetSafe(key);
        }
    }
}
