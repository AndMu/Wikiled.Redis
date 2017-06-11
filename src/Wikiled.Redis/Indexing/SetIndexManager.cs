using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Core.Utility.Helpers;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Indexing
{
    public class SetIndexManager : IndexManagerBase
    {
        private readonly IIndexKey index;

        public SetIndexManager(IRedisLink link, IDatabaseAsync database, IIndexKey index)
            : base(link, database, index)
        {
            Guard.NotNull(() => index, index);
            Guard.NotNull(() => link, link);
            Guard.NotNull(() => database, database);
            this.index = index;
        }

        public override Task AddRawIndex(string rawKey)
        {
            Guard.NotNullOrEmpty(() => rawKey, rawKey);
            return Database.SortedSetAddAsync(Link.GetIndexKey(index), rawKey, DateTime.UtcNow.ToUnixTime());
        }

        public override Task<long> Count()
        {
            return Database.SortedSetLengthAsync(Link.GetIndexKey(index));
        }

        public override IObservable<RedisValue> GetIds(long start = 0, long stop = -1)
        {
            return Observable.Create<RedisValue>(
                async observer =>
                {
                    var keys = await Database.SortedSetRangeByRankAsync(Link.GetIndexKey(index), start, stop, Order.Descending).ConfigureAwait(false);
                    foreach(var key in keys)
                    {
                        observer.OnNext(key);
                    }

                    observer.OnCompleted();
                });
        }
    }
}
