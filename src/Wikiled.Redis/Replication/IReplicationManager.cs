using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Information;

namespace Wikiled.Redis.Replication
{
    public interface IReplicationManager : IChannel
    {
        event EventHandler<ReplicationEventArgs> StepCompleted;

        EndPoint Master { get; }

        Task<IReplicationInfo> Perform(CancellationToken token);
    }
}