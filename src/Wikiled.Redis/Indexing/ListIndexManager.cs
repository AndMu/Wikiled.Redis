using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Logic.Resilience;

namespace Wikiled.Redis.Indexing
{
    public class ListIndexManager : IndexManagerBase
    {
        public ListIndexManager(IRedisLink link,  params IIndexKey[] indexes)
            : base(link, indexes)
        {
        }

        protected override Task RemoveRawIndex(IDatabaseAsync database, IIndexKey index, string rawKey)
        {
            // not supported
            return Task.CompletedTask;
        }

        protected override Task AddRawIndex(IDatabaseAsync database, IIndexKey index, string rawKey)
        {
            return database.ListLeftPushAsync(Link.GetIndexKey(index), rawKey);
        }

        protected override Task<long> SingleCount(IDatabaseAsync database, IIndexKey index)
        {
            return database.ListLengthAsync(Link.GetIndexKey(index));
        }

        protected override IObservable<RedisValue> GetIdsSingle(IDatabaseAsync database, IIndexKey index, long start = 0, long stop = -1)
        {
            return Observable.Create<RedisValue>(
                async observer =>
                {

                    var keys = await Link.Resilience.AsyncRetryPolicy
                                                 .ExecuteAsync(async () => await database.ListRangeAsync(Link.GetIndexKey(index), start, stop).ConfigureAwait(false))
                                                 .ConfigureAwait(false);

                    foreach(var key in keys)
                    {
                        observer.OnNext(key);
                    }

                    observer.OnCompleted();
                });
        }
    }
}
