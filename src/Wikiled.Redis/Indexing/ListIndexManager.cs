using System;
using System.Diagnostics.Contracts;
using System.Reactive.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Indexing
{
    public class ListIndexManager : IndexManagerBase
    {
        private readonly IIndexKey index;

        public ListIndexManager(IRedisLink link, IDatabaseAsync database, IIndexKey index)
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
            return Database.ListLeftPushAsync(Link.GetIndexKey(index), rawKey);
        }

        public override Task<long> Count()
        {
            return Database.ListLengthAsync(Link.GetIndexKey(index));
        }

        public override IObservable<RedisValue> GetIds(long start = 0, long stop = -1)
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
