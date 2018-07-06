
namespace Wikiled.Redis.Information
{
    public class StatsInfo : BaseInformation, IStatsInfo
    {
        public const string Name = "Stats";

        public StatsInfo(IServerInformation main)
            : base(main, Name)
        {
            if (main == null)
            {
                throw new System.ArgumentNullException(nameof(main));
            }

            TotalCommands = GetType<long>("total_commands_processed");
        }

        public long? TotalCommands { get; }
    }
}
