using System;
using System.Net;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Redis.Information;

namespace Wikiled.Redis.Replication
{
    public class ReplicationEventArgs : EventArgs
    {
        public ReplicationEventArgs(EndPoint server, IReplicationInfo status)
        {
            Guard.NotNull(() => status, status);
            Guard.NotNull(() => server, server);
            Status = status;
            Server = server;
        }

        public EndPoint Server { get; }

        public IReplicationInfo Status { get; }
    }
}
