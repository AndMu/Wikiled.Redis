using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        private readonly IMainIndexManager mainIndexManager;

        private readonly ILogger<RedisClient> logger;

        public RedisClient(ILogger<RedisClient> logger, IRedisLink link, IMainIndexManager mainIndexManager,IDatabaseAsync database = null)
        {
            this.link = link ?? throw new ArgumentNullException(nameof(link));
            this.mainIndexManager = mainIndexManager ?? throw new ArgumentNullException(nameof(mainIndexManager));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

            return  link.Resilience
                        .AsyncRetryPolicy
                        .ExecuteAsync(async () => await link.GetSpecific<T>().AddRecord(GetDatabase(), key, instances).ConfigureAwait(false));
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

            return link.Resilience
                       .AsyncRetryPolicy
                       .ExecuteAsync(async () => await link.GetSpecific<T>().AddRecords(GetDatabase(), keys, instances).ConfigureAwait(false));
        }

        public Task<bool> ContainsRecord<T>(IDataKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return link.Resilience
                       .AsyncRetryPolicy
                       .ExecuteAsync(async () => await link.ContainsRecord(GetDatabase(), key).ConfigureAwait(false));
        }

        public Task DeleteAll<T>(IDataKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return link.Resilience
                       .AsyncRetryPolicy
                       .ExecuteAsync(async () => await link.GetSpecific<T>().DeleteAll(GetDatabase(), key).ConfigureAwait(false));
        }

        public Task SetExpire<T>(IDataKey key, TimeSpan span)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return link.Resilience
                       .AsyncRetryPolicy
                       .ExecuteAsync(async () => await link.GetSpecific<T>().SetExpire(GetDatabase(), key, span).ConfigureAwait(false));
        }

        public Task SetExpire<T>(IDataKey key, DateTime dateTime)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return link.Resilience
                       .AsyncRetryPolicy
                       .ExecuteAsync(async () => await link.GetSpecific<T>().SetExpire(GetDatabase(), key, dateTime).ConfigureAwait(false));
        }

        public Task<long> Count(IIndexKey index)
        {
            if (index == null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            var indexManager = mainIndexManager.GetManager(index);
            if (indexManager == null)
            {
                return Task.FromResult(0L);
            }

            return link.Resilience
                       .AsyncRetryPolicy
                       .ExecuteAsync(async () => await indexManager.Count(GetDatabase()).ConfigureAwait(false));
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

            logger.LogDebug($"GetRecords {start}-{end}");
            var indexManager = mainIndexManager.GetManager(index);
            int batch = BatchSize;
            if (index.Length > 1)
            {
                logger.LogWarning("Joined index batching is not supported");
                batch = int.MaxValue;
            }

            return ObserverHelpers.Batch(
                indexManager.Count(GetDatabase()),
                (fromIndex, toIndex) => indexManager.GetKeys(GetDatabase(), fromIndex, toIndex),
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

        public Task<long> Count<T>(IDataKey dataKey)
        {
            return link.Resilience
                       .AsyncRetryPolicy
                       .ExecuteAsync(async () => await link.GetSpecific<T>().Count(GetDatabase(), dataKey).ConfigureAwait(false));
        }

        private IDatabaseAsync GetDatabase()
        {
            return database ?? link.Database;
        }
    }
}
