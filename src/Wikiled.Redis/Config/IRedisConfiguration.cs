using StackExchange.Redis;

namespace Wikiled.Redis.Config
{
    public interface IRedisConfiguration
    {
        string ServiceName { get; }

        ConfigurationOptions GetOptions();

        bool AbortOnConnectFail { get; set; }

        bool AllowAdmin { get; set; }

        int ConnectRetry { get; set; }

        int ConnectTimeout { get; set; }

        RedisEndpoint[] Endpoints { get; set; }

        int KeepAlive { get; set; }

        int ResponseTimeout { get; set; }

        string Password { get; set; }

        int SyncTimeout { get; set; }

        int WriteBuffer { get; set; }
    }
}
