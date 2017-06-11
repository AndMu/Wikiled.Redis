using Wikiled.Core.Utility.Arguments;
using Wikiled.Redis.Config;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.UnitTests.Logic
{
    public class SimpleRedisFactory : IRedisFactory
    {
        public IRedisMultiplexer Create(RedisConfiguration configuration)
        {
            Guard.NotNull(() => configuration, configuration);
            var instance = new RedisMultiplexer(configuration);
            instance.Open();
            return instance; 
        }
    }
}
