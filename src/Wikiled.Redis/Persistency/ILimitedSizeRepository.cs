namespace Wikiled.Redis.Persistency
{
    public interface ILimitedSizeRepository : IRepository
    {
        long Size { get; }
    }
}
