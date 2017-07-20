using Wikiled.Redis.Channels;

namespace Wikiled.Redis.Logic.Pool
{
    public interface IRedisLinksPool : IChannel
    {
        IRedisLink GetKey(string key);
    }
}