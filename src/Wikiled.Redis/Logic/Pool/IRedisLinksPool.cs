using Wikiled.Redis.Channels;

namespace Wikiled.Redis.Logic.Pool
{
    public interface IRedisLinksPool
        : IChannel
    {
        string Name { get; }

        ChannelState State { get; }

        void Close();

        void Dispose();

        void Open();
    }
}