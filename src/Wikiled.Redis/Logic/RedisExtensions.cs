using StackExchange.Redis;
using System;
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
    }
}
