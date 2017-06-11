namespace Wikiled.Redis.Information
{
    public interface IReplicationInfo
    {
        ReplicationRole? Role { get; }

        MasterLinkStatus? MasterLinkStatus { get; }

        long? LastSync { get; }

        byte? IsMasterSyncInProgress { get; }

        long? SlaveReplOffset { get;  }
    }
}