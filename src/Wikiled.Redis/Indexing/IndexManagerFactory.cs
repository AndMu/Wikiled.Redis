using System;
using StackExchange.Redis;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Indexing
{
    public class IndexManagerFactory : IIndexManagerFactory
    {
        private readonly IRedisLink link;

        public IndexManagerFactory(IRedisLink link)
        {
            this.link = link ?? throw new ArgumentNullException(nameof(link));
        }

        public IIndexManager Create(IDatabaseAsync database, params IIndexKey[] index)
        {
            if (index == null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            if (index.Length == 0)
            {
                throw new ArgumentException("Value cannot be an empty collection.", nameof(index));
            }

            var indexKey = index[0] as IndexKey;
            var hashIndex = index[0] as HashIndexKey;
            if (indexKey != null)
            {
                return indexKey.IsSet
                           ? (IIndexManager)new SetIndexManager(link, database, index)
                           : new ListIndexManager(link, database, index);
            }

            if (hashIndex != null)
            {
                return new HashIndexManager(link, database, index);
            }

            throw new NotSupportedException("Indexing type is not supported: " + index);
        }
    }
}
