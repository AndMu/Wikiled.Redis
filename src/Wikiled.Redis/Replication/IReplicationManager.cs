using System;
using Wikiled.Redis.Channels;

namespace Wikiled.Redis.Replication
{
    public interface IReplicationManager : IChannel
    {
        IObservable<ReplicationProgress> Progress { get; }
    }
}