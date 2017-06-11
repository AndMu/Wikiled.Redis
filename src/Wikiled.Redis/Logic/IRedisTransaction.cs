using System.Threading.Tasks;

namespace Wikiled.Redis.Logic
{
    public interface IRedisTransaction
    {
        IRedisClient Client { get; }

        Task Commit();
    }
}
