using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Indexing
{
    public class ListIndexManager : IndexManagerBase
    {
        public ListIndexManager(ILogger<ListIndexManager> logger, IRedisLink link)
            : base(logger, link)
        {
        }

        public override Task<long> Count(IDatabaseAsync database, IIndexKey index)
        {
            return database.ListLengthAsync(Link.GetIndexKey(index));
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
