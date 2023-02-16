using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Indexing
{
    public abstract class IndexManagerBase : IIndexManager
    {
        protected IndexManagerBase(ILogger log, IRedisLink link)
        {
            Link = link ?? throw new ArgumentNullException(nameof(link));
            Logger = log ?? throw new ArgumentNullException(nameof(log));
        }

        protected ILogger Logger { get; }

        protected IRedisLink Link { get; }

        public Task AddIndex(IDatabaseAsync database, IDataKey key, IIndexKey indexes)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return AddRawIndex(database, key.RecordId, indexes);
        }

        public Task AddRawIndex(IDatabaseAsync database, string rawKey, IIndexKey indexes)
        {
            if (string.IsNullOrEmpty(rawKey))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(rawKey));
            }

            return AddRawIndex(database, indexes, rawKey);
        }

        public Task RemoveIndex(IDatabaseAsync database, IDataKey key, IIndexKey indexes)
        {
            if (key?.RecordId == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return RemoveRawIndex(database, indexes, key.RecordId);
        }

        public abstract Task<long> Count(IDatabaseAsync database, IIndexKey index);

        public IObservable<IDataKey> GetIds(IDatabaseAsync database, IIndexKey indexes, long start = 0, long stop = -1)
        {
            return GetIdsSingle(database, indexes, start, stop);
        }

        public IObservable<IDataKey> GetKeys(IDatabaseAsync database, IIndexKey indexes, long start = 0, long stop = -1)
        {
            Logger.LogTrace("GetKeys");
            var keys = GetIds(database, indexes, start, stop);
            return keys;
        }

        public Task Reset(IDatabaseAsync database, IIndexKey indexes)
        {
            Logger.LogDebug("Reset");
            return database.KeyDeleteAsync(Link.GetIndexKey(indexes));
        }

        protected abstract Task RemoveRawIndex(IDatabaseAsync database, IIndexKey index, string rawKey);

        protected abstract Task AddRawIndex(IDatabaseAsync database, IIndexKey index, string rawKey);

        protected abstract IObservable<IDataKey> GetIdsSingle(IDatabaseAsync database, IIndexKey key, long start = 0, long stop = -1);

        protected IDataKey GetKey(string key, IIndexKey indexes)
        {
            var repository = indexes.RepositoryKey;
            if (!string.IsNullOrEmpty(repository))
            {
                return SimpleKey.GenerateKey(repository, key);
            }

            return new ObjectKey(key);
        }
    }
}
