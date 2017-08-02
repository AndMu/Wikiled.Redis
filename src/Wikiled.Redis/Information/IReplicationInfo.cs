namespace Wikiled.Redis.Information
{
    public interface IReplicationInfo
    {
        SlaveInformation[] Slaves { get; }

        long? MasterReplOffset { get; }

        ReplicationRole? Role { get; }

        MasterLinkStatus? MasterLinkStatus { get; }

        long? LastSync { get; }

        bool? IsMasterSyncInProgress { get; }

        long? SlaveReplOffset { get;  }
    }
}