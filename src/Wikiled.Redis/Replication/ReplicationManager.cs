﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Redis.Information;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Replication
{
    public class ReplicationManager : TimerChannel, IReplicationManager
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly IRedisMultiplexer multiplexer;

        private readonly Dictionary<string, long> lastSyncTable = new Dictionary<string, long>();

        public ReplicationManager(IRedisMultiplexer multiplexer, EndPoint master, TimeSpan scanStatus)
            : base("Replication", scanStatus)
        {
            Guard.NotNull(() => multiplexer, multiplexer);
            Guard.NotNull(() => master, master);
            this.multiplexer = multiplexer;
            Master = master;
        }

        public event EventHandler<ReplicationEventArgs> StepCompleted;

        public EndPoint Master { get; }

        public async Task<IReplicationInfo> Perform(CancellationToken token)
        {
            if (State != ChannelState.New)
            {
                throw new InvalidOperationException("Manager is already activated");
            }

            TaskCompletionSource<IReplicationInfo> task = new TaskCompletionSource<IReplicationInfo>();

            StepCompleted += (sender, args) =>
            {
                task.SetResult(args.Status);
            };

            Open();
            await Task.WhenAny(task.Task, Task.Delay(Timeout.Infinite, token));
            Close();
            if (token.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }

            return await task.Task;
        }

        protected override void TimerEvent()
        {
            var info = multiplexer.GetInfo(ReplicationInfo.Name);
            foreach (var information in info)
            {
                if (information.Replication.IsMasterSyncInProgress == 1 ||
                    information.Replication.LastSync < 1)
                {
                    continue;
                }
                
                if (information.Replication.SlaveReplOffset == null)
                {
                    log.Error("No offset information found");
                    continue;
                }

                var server = information.Server.ToString();
                long txOffset;
                if (lastSyncTable.TryGetValue(server, out txOffset) &&
                    txOffset == information.Replication.SlaveReplOffset)
                {
                    continue;
                }

                lastSyncTable[server] = information.Replication.SlaveReplOffset.Value;
                StepCompleted?.Invoke(this, new ReplicationEventArgs(information.Server, information.Replication));
            }
        }

        protected override ChannelState OpenInternal()
        {
            log.Debug("Making redis SLAVE OF {0}", Master);
            multiplexer.SetupSlave(Master);
            return base.OpenInternal();
        }

        protected override void CloseInternal()
        {
            log.Debug("Stopping Replication process");
            multiplexer.SetupSlave(null);
            base.CloseInternal();
        }
    }
}
