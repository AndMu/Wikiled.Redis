namespace Wikiled.Redis.Information
{
    public class MemoryInfo : BaseInformation, IMemoryInfo
    {
        public const string Memory = "Memory";

        public MemoryInfo(IServerInformation main)
            : base(main, Memory)
        {
            UsedMemory = GetType<long>("used_memory");
            MemoryFragmentation = GetType<double>("mem_fragmentation_ratio");
        }

        public double? MemoryFragmentation { get; }

        public long? UsedMemory { get; }
    }
}