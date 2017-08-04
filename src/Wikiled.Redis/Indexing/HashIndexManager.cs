using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Indexing
{
    public class HashIndexManager : IndexManagerBase
    {
        private readonly IIndexKey index;

        public HashIndexManager(IRedisLink link, IDatabaseAsync database, IIndexKey index)
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
            var hashIndex = (HashIndexKey)index;
            return Database.HashSetAsync(Link.GetIndexKey(index), new[] {new HashEntry(hashIndex.HashKey, rawKey)});
        }

        public override Task<long> Count()
        {
            return Database.HashLengthAsync(Link.GetIndexKey(index));
        }

        public override IObservable<RedisValue> GetIds(long start = 0, long stop = -1)
        {
            var hashKey = ((HashIndexKey)index).HashKey;
            return new[] {Link.Database.HashGet(Link.GetIndexKey(index), hashKey)}.ToObservable();
        }
    }
}
