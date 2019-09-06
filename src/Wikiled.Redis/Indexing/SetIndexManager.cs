using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using Wikiled.Common.Helpers;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Indexing
{
    public class SetIndexManager : IndexManagerBase
    {
        public SetIndexManager(IRedisLink link, IDatabaseAsync database, params IIndexKey[] indexes)
            : base(link, database, indexes)
        {
        }

        protected override Task RemoveRawIndex(IIndexKey index, string rawKey)
        {
            return Database.SortedSetRemoveAsync(Link.GetIndexKey(index), rawKey);
        }

        protected override Task AddRawIndex(IIndexKey index, string rawKey)
        {
            return Database.SortedSetAddAsync(Link.GetIndexKey(index), rawKey, DateTime.UtcNow.ToUnixTime());
        }

        protected override Task<long> SingleCount(IIndexKey index)
        {
            return Database.SortedSetLengthAsync(Link.GetIndexKey(index));
        }

        protected override IObservable<RedisValue> GetIdsSingle(IIndexKey index, long start = 0, long stop = -1)
        {
            return Observable.Create<RedisValue>(
                async observer =>
                {
                    var keys = await Database.SortedSetRangeByRankAsync(Link.GetIndexKey(index), start, stop, Order.Descending).ConfigureAwait(false);
                    foreach (var key in keys)
                    {
                        observer.OnNext(key);
                    }

                    observer.OnCompleted();
                });
        }
    }
}
