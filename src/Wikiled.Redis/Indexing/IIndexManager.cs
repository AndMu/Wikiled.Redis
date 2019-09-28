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
        /// <param name="database"></param>
        /// <param name="key"></param>
        /// <param name="indexes"></param>
        Task AddIndex(IDatabaseAsync database, IDataKey key, IIndexKey indexes);

        Task RemoveIndex(IDatabaseAsync database, IDataKey key, IIndexKey indexes);

        /// <summary>
        ///     Add raw index
        /// </summary>
        /// <param name="database"></param>
        /// <param name="rawKey"></param>
        /// <param name="indexes"></param>
        /// <returns></returns>
        Task AddRawIndex(IDatabaseAsync database, string rawKey, IIndexKey indexes);

        /// <summary>
        ///     Count records in index
        /// </summary>
        /// <returns></returns>
        Task<long> Count(IDatabaseAsync database, IIndexKey indexes);

        /// <summary>
        ///     Get ids in index
        /// </summary>
        /// <returns></returns>
        IObservable<RedisValue> GetIds(IDatabaseAsync database, IIndexKey indexes, long start = 0, long stop = -1);

        /// <summary>
        ///     Get Keys
        /// </summary>
        /// <returns></returns>
        IObservable<IDataKey> GetKeys(IDatabaseAsync database, IIndexKey indexes, long start = 0, long stop = -1);

        /// <summary>
        ///    Reset
        /// </summary>
        Task Reset(IDatabaseAsync database, IIndexKey indexes);
    }
}
