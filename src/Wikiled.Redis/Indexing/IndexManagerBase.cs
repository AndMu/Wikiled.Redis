using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NLog;
using StackExchange.Redis;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Indexing
{
    public abstract class IndexManagerBase : IIndexManager
    {
        private readonly IIndexKey index;

        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        protected IndexManagerBase(IRedisLink link, IDatabaseAsync database, IIndexKey index)
        {
            Guard.NotNull(() => index, index);
            Guard.NotNull(() => link, link);
            Guard.NotNull(() => database, database);
            Link = link;
            Database = database;
            this.index = index;
        }

        protected IDatabaseAsync Database { get; }

        protected IRedisLink Link { get; }

        public abstract Task AddRawIndex(string rawKey);

        public abstract Task<long> Count();

        public abstract IObservable<RedisValue> GetIds(long start = 0, long stop = -1);

        public Task AddIndex(IDataKey key)
        {
            Guard.NotNull(() => key, key);
            return AddRawIndex(key.RecordId);
        }

        public IObservable<IDataKey> GetKeys(long start = 0, long stop = -1)
        {
            log.Debug("GetKeys {0}", index);
            var keys = GetIds(start, stop);
            return keys.Select(item => GetKey(item));
        }

        public Task Reset()
        {
            log.Debug("Reset {0}", index);
            return Database.KeyDeleteAsync(Link.GetIndexKey(index));
        }

        private IDataKey GetKey(string key)
        {
            if(!string.IsNullOrEmpty(index.RepositoryKey))
            {
                return SimpleKey.GenerateKey(index.RepositoryKey, key);
            }

            return new ObjectKey(key);
        }
    }
}
