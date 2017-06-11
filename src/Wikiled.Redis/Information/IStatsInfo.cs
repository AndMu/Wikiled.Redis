namespace Wikiled.Redis.Information
{
    public interface IStatsInfo
    {
        long? TotalCommands { get; }
    }
}