using System;
using System.Threading.Tasks;
using StackExchange.Redis;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Serialization
{
    public static class BasicOperations
    {
        public static Task<bool> ContainsRecord(this IRedisLink link, IDatabaseAsync database, IDataKey key)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var actualKey = link.GetKey(key);
            return database.KeyExistsAsync(actualKey);
        }

        public static Task DeleteAll(this IRedisLink link, IDatabaseAsync database, IDataKey key)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var actualKey = link.GetKey(key);
            return database.KeyDeleteAsync(actualKey);
        }

        public static Task SetExpire(this IRedisLink link, IDatabaseAsync database, IDataKey key, TimeSpan time)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var actualKey = link.GetKey(key);
            return database.KeyExpireAsync(actualKey, time);
        }

        public static Task SetExpire(this IRedisLink link, IDatabaseAsync database, IDataKey key, DateTime dateTime)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var actualKey = link.GetKey(key);
            return database.KeyExpireAsync(actualKey, dateTime);
        }
    }
}
