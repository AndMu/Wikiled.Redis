using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using NLog;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Redis.Information;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Replication
{
    public class ReplicationManager : BaseChannel, IReplicationManager
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly IRedisMultiplexer slave;

        private readonly IRedisMultiplexer master;

        private EndPoint masterEndPoint;

        public ReplicationManager(
            IRedisMultiplexer master,
            IRedisMultiplexer slave,
            IObservable<long> timer)
            : base("Replication")
        {
            Guard.NotNull(() => slave, slave);
            Guard.NotNull(() => master, master);
            Guard.NotNull(() => timer, timer);
            this.slave = slave;
            this.master = master;
            Progress = timer.Select(TimerEvent);
        }

        public IObservable<ReplicationProgress> Progress { get; }

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
                log.Error(message);
                throw new InvalidOperationException(message);
            }

            var information = info[0];
            var masterOffset = information.Replication.MasterReplOffset;
            if (information.Replication.Role != ReplicationRole.Master ||
                masterOffset == null ||
                information.Replication.Slaves == null ||
                information.Replication.Slaves.Length < slave.GetServers().Count())
            {
                return ReplicationProgress.CreateInActive();
            }

            List<HostStatus> slaves = new List<HostStatus>();
            foreach (var slaveInformation in information.Replication.Slaves)
            {
                slaves.Add(new HostStatus(slaveInformation.EndPoint, slaveInformation.Offset));
            }

            return ReplicationProgress.CreateActive(
                new HostStatus(masterEndPoint, masterOffset.Value),
                slaves.ToArray());
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
            log.Debug("Making redis SLAVE OF {0}", servers[0].EndPoint);
            slave.SetupSlave(servers[0].EndPoint);
            return base.OpenInternal();
        }

        protected override void CloseInternal()
        {
            log.Debug("Stopping Replication process");
            slave.SetupSlave(null);
            base.CloseInternal();
        }
    }
}
