using System.Threading.Tasks;
using StackExchange.Redis;

namespace Wikiled.Redis.Logic
{
    public interface IRedisTransaction
    {
        IRedisClient Client { get; }

        ITransaction Transaction { get; }

        Task Commit();
    }
}
