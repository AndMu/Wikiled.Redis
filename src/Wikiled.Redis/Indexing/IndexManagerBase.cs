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

        protected IndexManagerBase(IRedisLink link, params IIndexKey[] indexes)
        {
            Link = link ?? throw new ArgumentNullException(nameof(link));
            this.indexes = indexes ?? throw new ArgumentNullException(nameof(indexes));
            repository = indexes.Select(item => item.RepositoryKey).First();
        }

        protected IRedisLink Link { get; }

        public Task AddIndex(IDatabaseAsync database, IDataKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return AddRawIndex(database, key.RecordId);
        }

        public Task AddRawIndex(IDatabaseAsync database, string rawKey)
        {
            if (string.IsNullOrEmpty(rawKey))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(rawKey));
            }

            var tasks = indexes.Select(index => AddRawIndex(database, index, rawKey));
            return Task.WhenAll(tasks);
        }

        public Task RemoveIndex(IDatabaseAsync database, IDataKey key)
        {
            if (key?.RecordId == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var tasks = indexes.Select(index => RemoveRawIndex(database, index, key.RecordId));
            return Task.WhenAll(tasks);
        }

        public async Task<long> Count(IDatabaseAsync database)
        {
            var tasks = indexes.Select(item => SingleCount(database, item));
            var result = await Task.WhenAll(tasks).ConfigureAwait(false);
            return result.Sum();
        }

        public IObservable<RedisValue> GetIds(IDatabaseAsync database, long start = 0, long stop = -1)
        {
            IObservable<RedisValue> result = null;
            foreach (var currentIndex in indexes)
            {
                var current = GetIdsSingle(database, currentIndex, start, stop);
                result = result != null ? result.InnerJoin(current) : current;
            }

            return result;
        }

        public IObservable<IDataKey> GetKeys(IDatabaseAsync database, long start = 0, long stop = -1)
        {
            log.LogDebug("GetKeys");
            var keys = GetIds(database, start, stop);
            return keys.Select(item => GetKey(item));
        }

        public Task Reset(IDatabaseAsync database)
        {
            log.LogDebug("Reset");
            var tasks = indexes.Select(item => database.KeyDeleteAsync(Link.GetIndexKey(item)));
            return Task.WhenAll(tasks);
        }

        protected abstract Task RemoveRawIndex(IDatabaseAsync database, IIndexKey index, string rawKey);

        protected abstract Task AddRawIndex(IDatabaseAsync database, IIndexKey index, string rawKey);

        protected abstract IObservable<RedisValue> GetIdsSingle(IDatabaseAsync database, IIndexKey key, long start = 0, long stop = -1);

        protected abstract Task<long> SingleCount(IDatabaseAsync database, IIndexKey index);

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
