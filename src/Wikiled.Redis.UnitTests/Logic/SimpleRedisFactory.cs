using System;
using Wikiled.Redis.Config;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.UnitTests.Logic
{
    public class SimpleRedisFactory : IRedisFactory
    {
        public IRedisMultiplexer Create(RedisConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var instance = new RedisMultiplexer(configuration);
            instance.Open();
            return instance; 
        }
    }
}
