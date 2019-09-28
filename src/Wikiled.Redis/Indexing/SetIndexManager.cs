using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using Wikiled.Common.Helpers;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Logic.Resilience;

namespace Wikiled.Redis.Indexing
{
    public class SetIndexManager : IndexManagerBase
    {
        public SetIndexManager(IRedisLink link, params IIndexKey[] indexes)
            : base(link, indexes)
        {
        }

        protected override Task RemoveRawIndex(IDatabaseAsync database, IIndexKey index, string rawKey)
        {
            return database.SortedSetRemoveAsync(Link.GetIndexKey(index), rawKey);
        }

        protected override Task AddRawIndex(IDatabaseAsync database, IIndexKey index, string rawKey)
        {
            return database.SortedSetAddAsync(Link.GetIndexKey(index), rawKey, DateTime.UtcNow.ToUnixTime());
        }

        protected override Task<long> SingleCount(IDatabaseAsync database, IIndexKey index)
        {
            return database.SortedSetLengthAsync(Link.GetIndexKey(index));
        }

        protected override IObservable<RedisValue> GetIdsSingle(IDatabaseAsync database, IIndexKey index, long start = 0, long stop = -1)
        {
            return Observable.Create<RedisValue>(
                async observer =>
                {
                    var keys = await Link.Resilience.AsyncRetryPolicy
                                         .ExecuteAsync(async () => await database.SortedSetRangeByRankAsync(Link.GetIndexKey(index), start, stop, Order.Descending).ConfigureAwait(false))
                                         .ConfigureAwait(false);

                    foreach (var key in keys)
                    {
                        observer.OnNext(key);
                    }

                    observer.OnCompleted();
                });
        }
    }
}
