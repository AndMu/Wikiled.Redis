namespace Wikiled.Redis.Information
{
    public interface IMemoryInfo
    {
        double? MemoryFragmentation { get; }

        long? UsedMemory { get; }
    }
}