using Wikiled.Redis.Config;

namespace Wikiled.Redis.Logic
{
    public interface IRedisFactory
    {
        IRedisMultiplexer Create(RedisConfiguration configuration);
    }
}
