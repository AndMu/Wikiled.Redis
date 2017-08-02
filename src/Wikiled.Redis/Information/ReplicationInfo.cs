using Wikiled.Core.Utility.Arguments;

namespace Wikiled.Redis.Information
{
    public class ReplicationInfo : BaseInformation, IReplicationInfo
    {
        public const string Name = "Replication";

        public ReplicationInfo(IServerInformation main)
            : base(main, Name)
        {
            Guard.NotNull(() => main, main);
            Role = GetType<ReplicationRole>("role");
            if (Role == ReplicationRole.Slave)
            {
                LastSync = GetType<long>("master_last_io_seconds_ago");
                MasterLinkStatus = GetType<MasterLinkStatus>("master_link_status");
                SlaveReplOffset = GetType<long>("slave_repl_offset");
                IsMasterSyncInProgress = GetType<byte>("master_sync_in_progress");
            }
            else if (Role == ReplicationRole.Master)
            {
                MasterReplOffset = GetType<long>("master_repl_offset");
                ConnectedSlaves = GetType<int>("connected_slaves");
                if (ConnectedSlaves != null)
                {
                    Slaves = new SlaveInformation[ConnectedSlaves.Value];
                    for (int i = 0; i < ConnectedSlaves.Value; i++)
                    {
                        Slaves[i] = SlaveInformation.Parse(GetType($"slave{i}"));
                    }
                }
            }
        }

        public SlaveInformation[] Slaves { get; }

        public long? MasterReplOffset { get; }

        public int? ConnectedSlaves { get; }

        public byte? IsMasterSyncInProgress { get; }

        public long? LastSync { get; }

        public MasterLinkStatus? MasterLinkStatus { get; }

        public ReplicationRole? Role { get; }

        public long? SlaveReplOffset { get; }
    }
}
