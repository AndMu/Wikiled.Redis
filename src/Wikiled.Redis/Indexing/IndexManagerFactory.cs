using System;
using StackExchange.Redis;
using Wikiled.Common.Arguments;
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

        public IIndexManager Create(params IIndexKey[] index)
        {
            Guard.NotNull(() => index, index);
            Guard.IsValid(() => index, index, keys => keys.Length > 0, "Provide at least one index");
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
