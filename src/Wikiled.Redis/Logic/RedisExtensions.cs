using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;
using Wikiled.Redis.Indexing;
using Wikiled.Redis.Keys;

namespace Wikiled.Redis.Logic
{
    public static class RedisExtensions
    {
        public static RedisKey GetIndexKey(this IRedisLink link, IIndexKey index)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            if (index == null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            return string.IsNullOrEmpty(index.RepositoryKey) ? link.GetKey(index.Key) : link.GetKey($"{index.RepositoryKey}:{index.Key}");
        }

        public static RedisKey GetKey(this IRedisLink link, IDataKey key)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return GetKey(link, key.FullKey);
        }

        public static RedisKey GetKey(this IRedisLink link, string key)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return link.Name + ":" + key;
        }

        public static Task[] Indexing(this IRedisLink link, IDatabaseAsync database, IDataKey dataKey)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            if (dataKey == null)
            {
                throw new ArgumentNullException(nameof(dataKey));
            }

            List<Task> tasks = new List<Task>();

            IndexManagerFactory factory = new IndexManagerFactory(link, database);
            foreach(var index in dataKey.Indexes)
            {
                tasks.Add(factory.Create(index).AddIndex(dataKey));
            }

            return tasks.ToArray();
        }
    }
}
