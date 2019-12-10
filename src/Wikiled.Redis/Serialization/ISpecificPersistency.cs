using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;
using Wikiled.Redis.Keys;

namespace Wikiled.Redis.Serialization
{
    public interface ISpecificPersistency<T>
    {
        /// <summary>
        ///     Add single key record
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="database"></param>
        /// <param name="key"></param>
        /// <param name="instances"></param>
        /// <returns></returns>
        Task AddRecord(IDatabaseAsync database, IDataKey key, params T[] instances);

        /// <summary>
        ///     Add records
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="database"></param>
        /// <param name="keys"></param>
        /// <param name="instances"></param>
        /// <returns></returns>
        Task AddRecords(IDatabaseAsync database, IEnumerable<IDataKey> keys, params T[] instances);

        /// <summary>
        ///     Remove all keys
        /// </summary>
        /// <param name="database"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        Task DeleteAll(IDatabaseAsync database, IDataKey key);

        /// <summary>
        /// Set Expire on key
        /// </summary>
        /// <param name="database"></param>
        /// <param name="key"></param>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        Task SetExpire(IDatabaseAsync database, IDataKey key, TimeSpan timeSpan);

        /// <summary>
        /// Set Expire on key
        /// </summary>
        /// <param name="database"></param>
        /// <param name="key"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        Task SetExpire(IDatabaseAsync database, IDataKey key, DateTime dateTime);

        /// <summary>
        ///     Get records
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="database"></param>
        /// <param name="dataKey"></param>
        /// <param name="fromRecord"></param>
        /// <param name="toRecord"></param>
        /// <returns></returns>
        IObservable<T> GetRecords(IDatabaseAsync database, IDataKey dataKey, long fromRecord = 0, long toRecord = -1);

        /// <summary>
        /// Count records
        /// </summary>
        /// <param name="database"></param>
        /// <param name="dataKey"></param>
        /// <returns></returns>
        Task<long> Count(IDatabaseAsync database, IDataKey dataKey);
    }
}
