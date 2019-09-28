using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Indexing
{
    public class HashIndexManager : IndexManagerBase
    {
        public HashIndexManager(ILogger<HashIndexManager> logger, IRedisLink link)
            : base(logger, link)
        {
        }

        public override Task<long> Count(IDatabaseAsync database, IIndexKey index)
        {
            return database.HashLengthAsync(Link.GetIndexKey(index));
        }

        protected override Task RemoveRawIndex(IDatabaseAsync database, IIndexKey index, string rawKey)
        {
            var hashIndex = (HashIndexKey)index;
            return database.HashDeleteAsync(Link.GetIndexKey(index), hashIndex.HashKey);
        }

        protected override Task AddRawIndex(IDatabaseAsync database, IIndexKey index, string rawKey)
        {
            var hashIndex = (HashIndexKey)index;
            return database.HashSetAsync(Link.GetIndexKey(index), new[] { new HashEntry(hashIndex.HashKey, rawKey) });
        }

        protected override IObservable<RedisValue> GetIdsSingle(IDatabaseAsync database, IIndexKey index, long start = 0, long stop = -1)
        {
            var hashKey = ((HashIndexKey) index).HashKey;

            return Observable.Create<RedisValue>(
                async observer =>
                {
                    var result = await Link.Resilience.AsyncRetryPolicy
                                                 .ExecuteAsync(async () => await database.HashGetAsync(Link.GetIndexKey(index), hashKey).ConfigureAwait(false))
                                                 .ConfigureAwait(false);
                    observer.OnNext(result);
                    observer.OnCompleted();
                });
        }
    }
}
