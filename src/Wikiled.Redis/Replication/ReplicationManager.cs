using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using Wikiled.Common.Logging;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Information;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Replication
{
    public class ReplicationManager : BaseChannel, IReplicationManager
    {
        private static readonly ILogger log = ApplicationLogging.CreateLogger<ReplicationManager>();

        private readonly IRedisMultiplexer master;

        private readonly IRedisMultiplexer slave;

        private EndPoint masterEndPoint;

        public ReplicationManager(IRedisMultiplexer master, IRedisMultiplexer slave, IObservable<long> timer)
            : base(log, "Replication")
        {
            if (timer == null)
            {
                throw new ArgumentNullException(nameof(timer));
            }

            this.slave = slave ?? throw new ArgumentNullException(nameof(slave));
            this.master = master ?? throw new ArgumentNullException(nameof(master));
            Progress = timer.Select(TimerEvent);
        }

        public IObservable<ReplicationProgress> Progress { get; }

        protected override void CloseInternal()
        {
            log.LogDebug("Stopping Replication process");
            slave.SetupSlave(null);
            base.CloseInternal();
        }

        protected override ChannelState OpenInternal()
        {
            if (!slave.IsActive)
            {
                throw new InvalidOperationException("Slave is not on");
            }

            if (!master.IsActive)
            {
                throw new InvalidOperationException("Master is not on");
            }

            var servers = master.GetServers().ToArray();
            if (servers.Length != 1)
            {
                throw new InvalidOperationException("Invalid Master server count");
            }

            masterEndPoint = servers[0].EndPoint;
            log.LogDebug("Making redis SLAVE OF {0}", servers[0].EndPoint);
            slave.SetupSlave(servers[0].EndPoint);
            return base.OpenInternal();
        }

        private static HostStatus[] GetSlaveInformation(IServerInformation information)
        {
            var slaves = new List<HostStatus>(information.Replication.Slaves.Length);
            foreach (var slaveInformation in information.Replication.Slaves)
            {
                slaves.Add(new HostStatus(slaveInformation.EndPoint, slaveInformation.Offset));
            }

            return slaves.ToArray();
        }

        private ReplicationProgress TimerEvent(long timer)
        {
            if (master == null ||
                !master.IsActive)
            {
                return ReplicationProgress.CreateInActive();
            }

            var info = master.GetInfo(ReplicationInfo.Name).ToArray();
            if (info.Length != 1)
            {
                string message = "Do not support zero or multiple masters replication: " + info.Length;
                log.LogError(message);
                throw new InvalidOperationException(message);
            }

            var information = info[0];
            var masterOffset = information.Replication.MasterReplOffset;
            if (information.Replication.Role != ReplicationRole.Master ||
                masterOffset == null ||
                information.Replication.Slaves == null ||
                information.Replication.Slaves.Length < slave.GetServers().Count())
            {
                log.LogDebug("Replication - Inactive");
                return ReplicationProgress.CreateInActive();
            }

            var slaves = GetSlaveInformation(information);
            log.LogDebug("Replication - Active");
            return ReplicationProgress.CreateActive(
                new HostStatus(masterEndPoint, masterOffset.Value),
                slaves.ToArray());
        }
    }
}
