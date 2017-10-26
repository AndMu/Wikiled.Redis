using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Redis.Indexing;
using Wikiled.Redis.Keys;

namespace Wikiled.Redis.Logic
{
    public static class RedisExtensions
    {
        public static RedisKey GetIndexKey(this IRedisLink link, IIndexKey index)
        {
            Guard.NotNull(() => link, link);
            Guard.NotNull(() => index, index);
            return string.IsNullOrEmpty(index.RepositoryKey) ? link.GetKey(index.Key) : link.GetKey($"{index.RepositoryKey}:{index.Key}");
        }

        public static RedisKey GetKey(this IRedisLink link, IDataKey key)
        {
            Guard.NotNull(() => link, link);
            Guard.NotNull(() => key, key);
            return GetKey(link, key.FullKey);
        }

        public static RedisKey GetKey(this IRedisLink link, string key)
        {
            Guard.NotNull(() => link, link);
            Guard.NotNull(() => key, key);
            return link.Name + ":" + key;
        }

        public static Task[] Indexing(this IRedisLink link, IDatabaseAsync database, IDataKey dataKey)
        {
            Guard.NotNull(() => link, link);
            Guard.NotNull(() => dataKey, dataKey);
            Guard.NotNull(() => database, database);

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
