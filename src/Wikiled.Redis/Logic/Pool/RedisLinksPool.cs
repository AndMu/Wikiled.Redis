using System.Linq;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Config;

namespace Wikiled.Redis.Logic.Pool
{
    public class RedisLinksPool : IRedisLinksPool
    {
        private readonly RedisConfiguration[] configurations;

        private RedisLink[] links;

        private bool isDispossed;

        public RedisLinksPool(RedisConfiguration[] configurations)
        {
            Guard.NotNull(() => configurations, configurations);
            this.configurations = configurations;
            State = ChannelState.New;
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
                    redisLink.Open();
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
                    redisLink.Dispose();
                }
            }
        }

        public void Open()
        {
            State = ChannelState.Opening;
            links = configurations.Select(item => new RedisLink(item.ServiceName, new RedisMultiplexer(item))).ToArray();
            foreach (var redisLink in links)
            {
                redisLink.Open();
            }

            State = ChannelState.Open;
        }
    }
}
