using System.Threading.Tasks;
using StackExchange.Redis;
using Wikiled.Redis.Keys;

namespace Wikiled.Redis.Serialization
{
    public interface IRedisSetList
    {
        Task<long> GetLength(IDatabaseAsync database, RedisKey key);

        Task<RedisValue[]> GetRedisValues(IDatabaseAsync database, RedisKey key, long from = 0, long to = -1);

        Task SaveItems(IDatabaseAsync database, IDataKey key, params RedisValue[] redisValues);
    }
}
