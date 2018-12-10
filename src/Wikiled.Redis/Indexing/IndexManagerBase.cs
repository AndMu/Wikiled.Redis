using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Wikiled.Common.Logging;
using Wikiled.Redis.Helpers;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Indexing
{
    public abstract class IndexManagerBase : IIndexManager
    {
        private readonly IIndexKey[] indexes;

        private static readonly ILogger log = ApplicationLogging.CreateLogger<IndexManagerBase>();

        private readonly string repository;

        protected IndexManagerBase(IRedisLink link, IDatabaseAsync database, params IIndexKey[] indexes)
        {
            Link = link ?? throw new ArgumentNullException(nameof(link));
            Database = database ?? throw new ArgumentNullException(nameof(database));
            this.indexes = indexes ?? throw new ArgumentNullException(nameof(indexes));
            repository = indexes.Select(item => item.RepositoryKey).First();
        }

        protected IDatabaseAsync Database { get; }

        protected IRedisLink Link { get; }

        public Task AddIndex(IDataKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return AddRawIndex(key.RecordId);
        }

        public Task AddRawIndex(string rawKey)
        {
            if (string.IsNullOrEmpty(rawKey))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(rawKey));
            }

            var tasks = indexes.Select(index => AddRawIndex(index, rawKey));
            return Task.WhenAll(tasks);
        }

        public async Task<long> Count()
        {
            var tasks = indexes.Select(SingleCount);
            var result = await Task.WhenAll(tasks).ConfigureAwait(false);
            return result.Sum();
        }

        public IObservable<RedisValue> GetIds(long start = 0, long stop = -1)
        {
            IObservable<RedisValue> result = null;
            foreach (var currentIndex in indexes)
            {
                var current = GetIdsSingle(currentIndex, start, stop);
                result = result != null ? result.InnerJoin(current) : current;
            }

            return result;
        }

        public IObservable<IDataKey> GetKeys(long start = 0, long stop = -1)
        {
            log.LogDebug("GetKeys {0}", indexes);
            var keys = GetIds(start, stop);
            return keys.Select(item => GetKey(item));
        }

        public Task Reset()
        {
            log.LogDebug("Reset {0}", indexes);
            var tasks = indexes.Select(item => Database.KeyDeleteAsync(Link.GetIndexKey(item)));
            return Task.WhenAll(tasks);
        }

        protected abstract Task AddRawIndex(IIndexKey index, string rawKey);

        protected abstract IObservable<RedisValue> GetIdsSingle(IIndexKey key, long start = 0, long stop = -1);

        protected abstract Task<long> SingleCount(IIndexKey index);

        private IDataKey GetKey(string key)
        {
            if (!string.IsNullOrEmpty(repository))
            {
                return SimpleKey.GenerateKey(repository, key);
            }

            return new ObjectKey(key);
        }
    }
}
