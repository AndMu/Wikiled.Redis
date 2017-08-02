namespace Wikiled.Redis.Information
{
    public interface IReplicationInfo
    {
        SlaveInformation[] Slaves { get; }

        long? MasterReplOffset { get; }

        ReplicationRole? Role { get; }

        MasterLinkStatus? MasterLinkStatus { get; }

        long? LastSync { get; }

        byte? IsMasterSyncInProgress { get; }

        long? SlaveReplOffset { get;  }
    }
}