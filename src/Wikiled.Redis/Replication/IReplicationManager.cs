using System;
using System.Net;
using Wikiled.Redis.Channels;

namespace Wikiled.Redis.Replication
{
    public interface IReplicationManager : IChannel
    {
        event EventHandler<ReplicationEventArgs> StepCompleted;

        EndPoint Master { get; }
    }
}