using System;
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

        public IIndexManager Create(params IIndexKey[] index)
        {
            if (index == null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            if (index.Length == 0)
            {
                throw new ArgumentException("Value cannot be an empty collection.", nameof(index));
            }

            switch (index[0])
            {
                case IndexKey indexKey:
                    return indexKey.IsSet
                        ? (IIndexManager)new SetIndexManager(link, index)
                        : new ListIndexManager(link, index);
                case HashIndexKey indexKey:
                    return new HashIndexManager(link, indexKey);
            }

            throw new NotSupportedException("Indexing type is not supported: " + index);
        }
    }
}
