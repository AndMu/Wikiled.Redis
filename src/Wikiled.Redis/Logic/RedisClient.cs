using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NLog;
using StackExchange.Redis;
using Wikiled.Redis.Helpers;
using Wikiled.Redis.Indexing;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Serialization;

namespace Wikiled.Redis.Logic
{
    public class RedisClient : IRedisClient
    {
        private readonly IDatabaseAsync database;

        private readonly IRedisLink link;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public RedisClient(IRedisLink link, IDatabaseAsync database = null)
        {
            this.link = link ?? throw new ArgumentNullException(nameof(link));
            this.database = database;
        }

        public int BatchSize { get; set; } = 10;

        public Task AddRecord<T>(IDataKey key, params T[] instances)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            return link.GetSpecific<T>().AddRecord(GetDatabase(), key, instances);
        }

        public Task AddRecords<T>(IEnumerable<IDataKey> keys, params T[] instances)
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            return link.GetSpecific<T>().AddRecords(GetDatabase(), keys, instances);
        }

        public Task<bool> ContainsRecord<T>(IDataKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return link.ContainsRecord(GetDatabase(), key);
        }

        public Task DeleteAll<T>(IDataKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return link.GetSpecific<T>().DeleteAll(GetDatabase(), key);
        }

        public Task SetExpire<T>(IDataKey key, TimeSpan span)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return link.GetSpecific<T>().SetExpire(GetDatabase(), key, span);
        }

        public Task SetExpire<T>(IDataKey key, DateTime dateTime)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return link.GetSpecific<T>().SetExpire(GetDatabase(), key, dateTime);
        }

        public IObservable<T> GetRecords<T>(IIndexKey index, long start = 0, long end = -1)
        {
            return GetRecords<T>(new[] { index }, start, end);
        }

        public IObservable<T> GetRecords<T>(IIndexKey[] index, long start = 0, long end = -1)
        {
            if (index == null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            logger.Debug($"GetRecords {start}-{end}");
            var indexManager = new IndexManagerFactory(link, GetDatabase()).Create(index);
            int batch = BatchSize;
            if (index.Length > 1)
            {
                logger.Warn("Joined index batching is not supported");
                batch = int.MaxValue;
            }

            return ObserverHelpers.Batch(
                indexManager.Count(),
                (fromIndex, toIndex) => indexManager.GetKeys(fromIndex, toIndex),
                GetRecords<T>,
                batch,
                start,
                end);
        }

        public IObservable<T> GetRecords<T>(IDataKey dataKey)
        {
            if (dataKey == null)
            {
                throw new ArgumentNullException(nameof(dataKey));
            }

            return link.GetSpecific<T>().GetRecords<T>(GetDatabase(), dataKey);
        }

        private IDatabaseAsync GetDatabase()
        {
            return database ?? link.Database;
        }
    }
}
