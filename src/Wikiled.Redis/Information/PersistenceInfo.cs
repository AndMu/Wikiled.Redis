using Wikiled.Core.Utility.Arguments;

namespace Wikiled.Redis.Information
{
    public class PersistenceInfo : BaseInformation, IPersistenceInfo
    {
        public const string Name = "Persistence";

        public PersistenceInfo(IServerInformation main)
            : base(main, Name)
        {
            Guard.NotNull(() => main, main);
            AofSize = GetType<long>("aof_current_size");
            IsRdbSaving = GetType<byte>("rdb_bgsave_in_progress");
            IsAofRewriting = GetType<byte>("aof_rewrite_in_progress");
        }

        public long? AofSize { get; }

        public byte? IsAofRewriting { get; }

        public byte? IsRdbSaving { get; }
    }
}
