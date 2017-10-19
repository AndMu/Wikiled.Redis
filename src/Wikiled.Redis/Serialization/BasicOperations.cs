using System;
using System.Threading.Tasks;
using StackExchange.Redis;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Serialization
{
    public static class BasicOperations
    {
        public static Task<bool> ContainsRecord(this IRedisLink link, IDatabaseAsync database, IDataKey key)
        {
            Guard.NotNull(() => database, database);
            Guard.NotNull(() => key, key);
            var actualKey = link.GetKey(key);
            return database.KeyExistsAsync(actualKey);
        }

        public static Task DeleteAll(this IRedisLink link, IDatabaseAsync database, IDataKey key)
        {
            Guard.NotNull(() => database, database);
            Guard.NotNull(() => key, key);
            var actualKey = link.GetKey(key);
            return database.KeyDeleteAsync(actualKey);
        }

        public static Task SetExpire(this IRedisLink link, IDatabaseAsync database, IDataKey key, TimeSpan time)
        {
            Guard.NotNull(() => database, database);
            Guard.NotNull(() => key, key);
            var actualKey = link.GetKey(key);
            return database.KeyExpireAsync(actualKey, time);
        }

        public static Task SetExpire(this IRedisLink link, IDatabaseAsync database, IDataKey key, DateTime dateTime)
        {
            Guard.NotNull(() => database, database);
            Guard.NotNull(() => key, key);
            var actualKey = link.GetKey(key);
            return database.KeyExpireAsync(actualKey, dateTime);
        }
    }
}
