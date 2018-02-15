using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using StackExchange.Redis;
using Wikiled.Common.Arguments;
using Wikiled.Common.Helpers;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Serialization
{
    public class RedisSet : IRedisSetList
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly IRedisLink link;

        public RedisSet(IRedisLink link)
        {
            Guard.NotNull(() => link, link);
            this.link = link;
        }

        public Task<long> GetLength(IDatabaseAsync database, RedisKey key)
        {
            return database.SortedSetLengthAsync(key);
        }

        public Task<RedisValue[]> GetRedisValues(IDatabaseAsync database, RedisKey key, long from, long to)
        {
            return database.SortedSetRangeByScoreAsync(key, from, to, order: Order.Descending);
        }

        public Task SaveItems(IDatabaseAsync database, IDataKey key, params RedisValue[] redisValues)
        {
            var redisKey = link.GetKey(key);
            log.Debug("AddSet: <{0}>", key);

            var time = DateTime.UtcNow.ToUnixTime();
            var saveTask = database.SortedSetAddAsync(
                redisKey,
                redisValues.Select(item => new SortedSetEntry(item, time)).ToArray());

            List<Task> tasks = new List<Task>(link.Indexing(database, key));
            tasks.Add(saveTask);
            return Task.WhenAll(tasks);
        }
    }
}
