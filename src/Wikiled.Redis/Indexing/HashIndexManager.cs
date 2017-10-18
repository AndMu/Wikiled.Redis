using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Indexing
{
    public class HashIndexManager : IndexManagerBase
    {
        public HashIndexManager(IRedisLink link, IDatabaseAsync database, params IIndexKey[] indexes)
            : base(link, database, indexes)
        {
        }

        protected override Task AddRawIndex(IIndexKey index, string rawKey)
        {
            var hashIndex = (HashIndexKey)index;
            return Database.HashSetAsync(Link.GetIndexKey(index), new[] { new HashEntry(hashIndex.HashKey, rawKey) });
        }

        protected override Task<long> SingleCount(IIndexKey index)
        {
            return Database.HashLengthAsync(Link.GetIndexKey(index));
        }

        protected override IObservable<RedisValue> GetIdsSingle(IIndexKey index, long start = 0, long stop = -1)
        {
            var hashKey = ((HashIndexKey)index).HashKey;
            return new[] { Link.Database.HashGet(Link.GetIndexKey(index), hashKey) }.ToObservable();
        }
    }
}
