namespace Wikiled.Redis.Information
{
    public interface IPersistenceInfo
    {
        long? AofSize { get; }

        byte? IsAofRewriting { get; }

        byte? IsRdbSaving { get; }
    }
}