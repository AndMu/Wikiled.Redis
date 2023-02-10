using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Indexing
{
    public class IndexManagerFactory : IIndexManagerFactory
    {
        private const string setName = "set";
        private const string hashName = "hash";

        private readonly ILoggerFactory loggerFactory;

        private readonly IRedisLink link;

        private Dictionary<string, IIndexManager> table = new Dictionary<string, IIndexManager>();

        public IndexManagerFactory(ILoggerFactory loggerFactory, IRedisLink link)
        {
            this.link = link ?? throw new ArgumentNullException(nameof(link));
            this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            table[setName] = new SetIndexManager(loggerFactory.CreateLogger<SetIndexManager>(), link);
            table[hashName] = new HashIndexManager(loggerFactory.CreateLogger<HashIndexManager>(), link);
        }

        public IIndexManager Create(IIndexKey index)
        {
            if (index == null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            switch (index)
            {
                case IndexKey _:
                    return table[setName];
                case HashIndexKey _:
                    return table[hashName];
            }

            throw new NotSupportedException("Indexing type is not supported: " + index);
        }
    }
}
