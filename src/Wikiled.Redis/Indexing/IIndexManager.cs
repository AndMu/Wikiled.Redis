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
        Task AddIndex(IDatabaseAsync database, IDataKey key);

        Task RemoveIndex(IDatabaseAsync database, IDataKey key);

        /// <summary>
        ///     Add raw index
        /// </summary>
        /// <param name="rawKey"></param>
        /// <returns></returns>
        Task AddRawIndex(IDatabaseAsync database, string rawKey);

        /// <summary>
        ///     Count records in index
        /// </summary>
        /// <returns></returns>
        Task<long> Count(IDatabaseAsync database);

        /// <summary>
        ///     Get ids in index
        /// </summary>
        /// <returns></returns>
        IObservable<RedisValue> GetIds(IDatabaseAsync database, long start = 0, long stop = -1);

        /// <summary>
        ///     Get Keys
        /// </summary>
        /// <returns></returns>
        IObservable<IDataKey> GetKeys(IDatabaseAsync database, long start = 0, long stop = -1);

        /// <summary>
        ///    Reset
        /// </summary>
        Task Reset(IDatabaseAsync database);
    }
}
