using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Redis.Information;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Config;
using Wikiled.Redis.Helpers;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Replication
{
    public class ReplicationManager : TimerChannel, IReplicationManager
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly IRedisMultiplexer slave;

        private Dictionary<string, long> lastSyncTable;

        private IRedisMultiplexer master;

        private readonly IRedisFactory factory;

        public ReplicationManager(IRedisFactory factory, IPEndPoint master, IRedisMultiplexer slave, TimeSpan scanStatus)
            : base("Replication", scanStatus)
        {
            Guard.NotNull(() => slave, slave);
            Guard.NotNull(() => master, master);
            Guard.NotNull(() => factory, factory);
            this.slave = slave;
            this.factory = factory;
            Master = master;
        }

        public event EventHandler<EventArgs> OnError;

        public event EventHandler<ReplicationEventArgs> OnCompleted;

        public IPEndPoint Master { get; }

        public async Task<IReplicationInfo> Perform(CancellationToken token)
        {
            if (State != ChannelState.New)
            {
                throw new InvalidOperationException("Manager is already activated");
            }

            TaskCompletionSource<IReplicationInfo> task = new TaskCompletionSource<IReplicationInfo>();

            OnCompleted += (sender, args) =>
            {
                task.SetResult(args.Status);
            };

            CancellationTokenSource errorToken = new CancellationTokenSource();
            OnError += (sender, args) =>
            {
                errorToken.Cancel();
            };

            Open();
            await Task.WhenAny(task.Task, Task.Delay(Timeout.Infinite, token), Task.Delay(Timeout.Infinite, errorToken.Token));
            Close();
            if (token.IsCancellationRequested ||
                errorToken.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }

            return await task.Task;
        }

        protected override void TimerEvent()
        {
            if (master == null ||
                !master.IsActive ||
                lastSyncTable == null)
            {
                return;
            }

            var info = master.GetInfo(ReplicationInfo.Name).ToArray();
            if (info.Length != 1)
            {
                log.Error("Do not support zero or multiple masters replication: " + info.Length);
                OnError?.Invoke(this, EventArgs.Empty);
                Close();
                return;
            }

            var information = info[0];
            var masterOffset = information.Replication.MasterReplOffset;
            if (information.Replication.Role != ReplicationRole.Master ||
                masterOffset == null ||
                information.Replication.Slaves == null)
            {
                return;
            }

            foreach (var slaveInformation in information.Replication.Slaves)
            {
                var key = slaveInformation.EndPoint.GetAddress();
                if (lastSyncTable.ContainsKey(key))
                {
                    lastSyncTable[key] = slaveInformation.Offset;
                }
            }

            if (lastSyncTable.All(item => item.Value == masterOffset.Value))
            {
                OnCompleted?.Invoke(this, new ReplicationEventArgs(information.Server, information.Replication));
                Close();
            }
        }

        protected override ChannelState OpenInternal()
        {
            log.Debug("Making redis SLAVE OF {0}", Master);
            lastSyncTable = slave.GetServers()
                                 .Select(item => item.EndPoint)
                                 .ToDictionary(
                                     item => item.GetAddress(),
                                     item => 0L);
            slave.SetupSlave(Master);
            master = factory.Create(new RedisConfiguration(Master.Address.ToString(), Master.Port));
            master.Open();
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
