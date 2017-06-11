using System;
using StackExchange.Redis;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Indexing
{
    public class IndexManagerFactory : IIndexManagerFactory
    {
        private readonly IDatabaseAsync database;

        private readonly IRedisLink link;

        public IndexManagerFactory(IRedisLink link, IDatabaseAsync database)
        {
            Guard.NotNull(() => link, link);
            Guard.NotNull(() => database, database);
            this.link = link;
            this.database = database;
        }

        public IIndexManager Create(IIndexKey index)
        {
            Guard.NotNull(() => index, index);
            var indexKey = index as IndexKey;
            var hashIndex = index as HashIndexKey;
            if (indexKey != null)
            {
                return index.IsSet
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
