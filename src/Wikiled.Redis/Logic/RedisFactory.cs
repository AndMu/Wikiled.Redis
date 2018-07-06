using System;
using System.Collections.Generic;
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
            this.internalFactory = internalFactory ?? throw new ArgumentNullException(nameof(internalFactory));
        }

        public IRedisMultiplexer Create(RedisConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (!configuration.PoolConnection)
            {
                return internalFactory.Create(configuration);
            }

            lock(syncObject)
            {
                if (poolTable.TryGetValue(configuration, out PooledRedisMultiplexer multiplexer))
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
                if (poolReverseTable.TryGetValue(multiplexer, out RedisConfiguration config))
                {
                    poolTable.Remove(config);
                }

                poolReverseTable.Remove(multiplexer);
            }
        }
    }
}
