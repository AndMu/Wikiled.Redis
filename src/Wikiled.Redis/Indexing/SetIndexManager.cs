using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Wikiled.Common.Helpers;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Indexing
{
    public class SetIndexManager : IndexManagerBase
    {
        public SetIndexManager(ILogger<SetIndexManager> logger, IRedisLink link)
            : base(logger, link)
        {
        }

        public override Task<long> Count(IDatabaseAsync database, IIndexKey index)
        {
            return database.SortedSetLengthAsync(Link.GetIndexKey(index));
        }

        protected override Task RemoveRawIndex(IDatabaseAsync database, IIndexKey index, string rawKey)
        {
            return database.SortedSetRemoveAsync(Link.GetIndexKey(index), rawKey);
        }

        protected override Task AddRawIndex(IDatabaseAsync database, IIndexKey index, string rawKey)
        {
            return database.SortedSetAddAsync(Link.GetIndexKey(index), rawKey, DateTime.UtcNow.ToUnixTime());
        }

        protected override IObservable<IDataKey> GetIdsSingle(IDatabaseAsync database, IIndexKey index, long start = 0, long stop = -1)
        {
            return Observable.Create<IDataKey>(
                async observer =>
                {
                    var indexKey = Link.GetIndexKey(index);
                    var reindexKey =  indexKey.Prepend(":reindex");
                    var reindex = await database.LockTakeAsync(reindexKey, "1", TimeSpan.FromHours(5));

                    var keys = await Link.Resilience.AsyncRetryPolicy
                                         .ExecuteAsync(async () => await database.SortedSetRangeByRankAsync(indexKey, start, stop, Order.Descending).ConfigureAwait(false))
                                         .ConfigureAwait(false);

                    foreach (var key in keys)
                    {
                        var dataKey = GetKey(key, index);
                        var generatedKey = Link.GetKey(GetKey(key, index));
                        
                        if (reindex &&
                            !await database.KeyExistsAsync(generatedKey))
                        {
                            Logger.LogWarning("Key {0} does not exist - removing on REINDEX", generatedKey);
                            await RemoveIndex(database, dataKey, index);
                        }
                        else
                        {
                            observer.OnNext(dataKey);
                        }

                    }

                    observer.OnCompleted();
                });
        }
    }
}
