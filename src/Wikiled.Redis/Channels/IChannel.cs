using System;

namespace Wikiled.Redis.Channels
{
    public interface IChannel : IDisposable
    {
        ChannelState State { get; }

        string Name { get; }

        void Close();

        void Open();
    }
}