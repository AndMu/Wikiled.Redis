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
            LastSync = GetType<long>("master_last_io_seconds_ago");
            MasterLinkStatus = GetType<MasterLinkStatus>("master_link_status");
            SlaveReplOffset = GetType<long>("slave_repl_offset");
            IsMasterSyncInProgress = GetType<byte>("master_sync_in_progress");
        }

        public byte? IsMasterSyncInProgress { get; }

        public long? LastSync { get; }

        public MasterLinkStatus? MasterLinkStatus { get; }

        public ReplicationRole? Role { get; }

        public long? SlaveReplOffset { get; }
    }
}
