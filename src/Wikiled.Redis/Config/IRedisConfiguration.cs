using StackExchange.Redis;

namespace Wikiled.Redis.Config
{
    public interface IRedisConfiguration
    {
        ConfigurationOptions GetOptions();
    }
}
