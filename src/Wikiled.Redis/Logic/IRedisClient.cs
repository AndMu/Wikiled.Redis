using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wikiled.Redis.Keys;

namespace Wikiled.Redis.Logic
{
    public interface IRedisClient
    {
        /// <summary>
        ///     Batch size
        /// </summary>
        int BatchSize { get; set; }

        /// <summary>
        ///     Add single key record
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        Task AddRecord<T>(IDataKey key, params T[] instance);

        /// <summary>
        ///     Add records
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keys"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        Task AddRecords<T>(IEnumerable<IDataKey> keys, params T[] instance);

        /// <summary>
        ///     Check is key in db
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<bool> ContainsRecord<T>(IDataKey key);

        /// <summary>
        ///     Delete all
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        Task DeleteAll<T>(IDataKey key);

        /// <summary>
        /// Set expire on key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="span"></param>
        /// <returns></returns>

        Task SetExpire<T>(IDataKey key, TimeSpan span);

        /// <summary>
        /// Set expire on key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>

        Task SetExpire<T>(IDataKey key, DateTime dateTime);

        /// <summary>
        ///     Get records helper method
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataKey"></param>
        /// <returns></returns>
        IObservable<T> GetRecords<T>(IDataKey dataKey);

        /// <summary>
        /// Count records
        /// </summary>
        /// <param name="dataKey"></param>
        /// <returns></returns>
        Task<long> Count<T>(IDataKey dataKey);

        /// <summary>
        /// Count index
        /// </summary>
        /// <param name="indexKey"></param>
        /// <returns></returns>
        Task<long> Count(IIndexKey indexKey);

        /// <summary>
        ///     Get records by index
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="index"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        IObservable<T> GetRecords<T>(IIndexKey index, long start = 0, long end = -1);
    }
}
