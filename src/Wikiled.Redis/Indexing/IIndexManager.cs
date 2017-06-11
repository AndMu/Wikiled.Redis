using System;
using System.Threading.Tasks;
using StackExchange.Redis;
using Wikiled.Redis.Keys;

namespace Wikiled.Redis.Indexing
{
    public interface IIndexManager
    {
        /// <summary>
        ///     Add index
        /// </summary>
        /// <param name="key"></param>
        Task AddIndex(IDataKey key);

        /// <summary>
        ///     Add raw index
        /// </summary>
        /// <param name="rawKey"></param>
        /// <returns></returns>
        Task AddRawIndex(string rawKey);

        /// <summary>
        ///     Count records in index
        /// </summary>
        /// <returns></returns>
        Task<long> Count();

        /// <summary>
        ///     Get ids in index
        /// </summary>
        /// <returns></returns>
        IObservable<RedisValue> GetIds(long start = 0, long stop = -1);

        /// <summary>
        ///     Get Keys
        /// </summary>
        /// <returns></returns>
        IObservable<IDataKey> GetKeys(long start = 0, long stop = -1);

        /// <summary>
        ///     Rset
        /// </summary>
        Task Reset();
    }
}
