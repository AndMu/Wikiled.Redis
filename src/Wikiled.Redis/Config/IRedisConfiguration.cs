using StackExchange.Redis;

namespace Wikiled.Redis.Config
{
    public interface IRedisConfiguration
    {
        string ServiceName { get; }

        ConfigurationOptions GetOptions();
    }
}
