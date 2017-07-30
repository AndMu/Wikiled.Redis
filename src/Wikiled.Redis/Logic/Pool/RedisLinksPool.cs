using System.Collections.Generic;
using System.Linq;
using NLog;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Core.Utility.Extensions;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Config;

namespace Wikiled.Redis.Logic.Pool
{
    public class RedisLinksPool : IRedisLinksPool
    {
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly RedisConfiguration[] configurations;

        private Dictionary<string, RedisLink> links;

        private bool isDispossed;

        public RedisLinksPool(RedisConfiguration[] configurations)
        {
            Guard.NotNull(() => configurations, configurations);
            this.configurations = configurations;
            State = ChannelState.New;
            logger.Debug("Adding {0} services", configurations.Length);
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
            if (isDispossed)
            {
                return;
            }

            isDispossed = true;
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
                logger.Debug("Opening {0}", redisLink.Value.Multiplexer.Configuration);
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
