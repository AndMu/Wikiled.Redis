using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NLog;
using StackExchange.Redis;
using Wikiled.Core.Utility.Arguments;
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
            Guard.NotNull(() => link, link);
            this.link = link;
            this.database = database;
        }

        public int BatchSize { get; set; } = 10;

        public Task AddRecord<T>(IDataKey key, params T[] instances)
        {
            Guard.NotNull(() => key, key);
            Guard.NotNull(() => instances, instances);
            return link.GetSpecific<T>().AddRecord(GetDatabase(), key, instances);
        }

        public Task AddRecords<T>(IEnumerable<IDataKey> keys, params T[] instances)
        {
            Guard.NotNull(() => keys, keys);
            Guard.NotNull(() => instances, instances);
            return link.GetSpecific<T>().AddRecords(GetDatabase(), keys, instances);
        }

        public Task<bool> ContainsRecord<T>(IDataKey key)
        {
            Guard.NotNull(() => key, key);
            return link.ContainsRecord(GetDatabase(), key);
        }

        public Task DeleteAll<T>(IDataKey key)
        {
            Guard.NotNull(() => key, key);
            return link.GetSpecific<T>().DeleteAll(GetDatabase(), key);
        }

        public IObservable<T> GetRecords<T>(IIndexKey index, long start = 0, long end = -1)
        {
            return GetRecords<T>(new[] { index }, start, end);
        }

        public IObservable<T> GetRecords<T>(IIndexKey[] index, long start = 0, long end = -1)
        {
            Guard.NotNull(() => index, index);
            logger.Debug($"GetRecords {start}-{end}");
            var indexManager = new IndexManagerFactory(link, GetDatabase()).Create(index);
            int batch = BatchSize;
            if (index.Length > 0)
            {
                logger.Debug("Joined index batching is not supported");
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
            Guard.NotNull(() => dataKey, dataKey);
            return link.GetSpecific<T>().GetRecords<T>(GetDatabase(), dataKey);
        }

        private IDatabaseAsync GetDatabase()
        {
            return database ?? link.Database;
        }
    }
}
