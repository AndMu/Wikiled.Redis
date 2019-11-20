using System;
using System.Threading.Tasks;

namespace Wikiled.Redis.Channels
{
    public interface IChannel : IDisposable
    {
        ChannelState State { get; }

        string Name { get; }

        void Close();

        Task Open();
    }
}