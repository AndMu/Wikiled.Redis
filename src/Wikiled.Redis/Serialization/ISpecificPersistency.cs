using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;
using Wikiled.Redis.Keys;

namespace Wikiled.Redis.Serialization
{
    public interface ISpecificPersistency
    {
        /// <summary>
        ///     Add single key record
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="database"></param>
        /// <param name="key"></param>
        /// <param name="instances"></param>
        /// <returns></returns>
        Task AddRecord<T>(IDatabaseAsync database, IDataKey key, params T[] instances);

        /// <summary>
        ///     Add records
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="database"></param>
        /// <param name="keys"></param>
        /// <param name="instances"></param>
        /// <returns></returns>
        Task AddRecords<T>(IDatabaseAsync database, IEnumerable<IDataKey> keys, params T[] instances);

        /// <summary>
        ///     Remove all keys
        /// </summary>
        /// <param name="database"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        Task DeleteAll(IDatabaseAsync database, IDataKey key);

        /// <summary>
        ///     Get records
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="database"></param>
        /// <param name="dataKey"></param>
        /// <param name="fromRecord"></param>
        /// <param name="toRecord"></param>
        /// <returns></returns>
        IObservable<T> GetRecords<T>(IDatabaseAsync database, IDataKey dataKey, long fromRecord = 0, long toRecord = -1);
    }
}
