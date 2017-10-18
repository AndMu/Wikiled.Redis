using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Indexing
{
    public class ListIndexManager : IndexManagerBase
    {
        public ListIndexManager(IRedisLink link, IDatabaseAsync database, params IIndexKey[] indexes)
            : base(link, database, indexes)
        {
        }

        protected override Task AddRawIndex(IIndexKey index, string rawKey)
        {
            return Database.ListLeftPushAsync(Link.GetIndexKey(index), rawKey);
        }

        protected override Task<long> SingleCount(IIndexKey index)
        {
            return Database.ListLengthAsync(Link.GetIndexKey(index));
        }

        protected override IObservable<RedisValue> GetIdsSingle(IIndexKey index, long start = 0, long stop = -1)
        {
            return Observable.Create<RedisValue>(
                async observer =>
                {
                    var keys = await Database.ListRangeAsync(Link.GetIndexKey(index), start, stop).ConfigureAwait(false);
                    foreach(var key in keys)
                    {
                        observer.OnNext(key);
                    }

                    observer.OnCompleted();
                });
        }
    }
}
