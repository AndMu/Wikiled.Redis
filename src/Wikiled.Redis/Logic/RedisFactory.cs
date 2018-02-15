using System;
using System.Collections.Generic;
using Wikiled.Common.Arguments;
using Wikiled.Redis.Config;

namespace Wikiled.Redis.Logic
{
    public class RedisFactory : IRedisFactory
    {
        private readonly IRedisFactory internalFactory;

        private static readonly Dictionary<PooledRedisMultiplexer, RedisConfiguration> poolReverseTable =
            new Dictionary<PooledRedisMultiplexer, RedisConfiguration>();

        private static readonly Dictionary<RedisConfiguration, PooledRedisMultiplexer> poolTable =
            new Dictionary<RedisConfiguration, PooledRedisMultiplexer>();

        private static readonly object syncObject = new object();

        public RedisFactory()
            : this(new SimpleRedisFactory())
        {
        }

        public RedisFactory(IRedisFactory internalFactory)
        {
            Guard.NotNull(() => internalFactory, internalFactory);
            this.internalFactory = internalFactory;
        }

        public IRedisMultiplexer Create(RedisConfiguration configuration)
        {
            Guard.NotNull(() => configuration, configuration);
            if (!configuration.PoolConnection)
            {
                return internalFactory.Create(configuration);
            }

            lock(syncObject)
            {
                PooledRedisMultiplexer multiplexer;
                if(poolTable.TryGetValue(configuration, out multiplexer))
                {
                    multiplexer.Increment();
                    return multiplexer;
                }

                var internalMultiplexer = internalFactory.Create(configuration);
                multiplexer = new PooledRedisMultiplexer(internalMultiplexer);
                poolTable[configuration] = multiplexer;
                poolReverseTable[multiplexer] = configuration;
                multiplexer.Released += MultiplexerOnReleased;
                return multiplexer;
            }
        }

        private void MultiplexerOnReleased(object sender, EventArgs eventArgs)
        {
            PooledRedisMultiplexer multiplexer = (PooledRedisMultiplexer)sender;
            lock(syncObject)
            {
                multiplexer.Released += MultiplexerOnReleased;
                RedisConfiguration config;
                if(poolReverseTable.TryGetValue(multiplexer, out config))
                {
                    poolTable.Remove(config);
                }

                poolReverseTable.Remove(multiplexer);
            }
        }
    }
}
