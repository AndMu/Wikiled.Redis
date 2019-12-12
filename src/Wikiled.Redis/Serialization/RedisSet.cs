using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Wikiled.Common.Helpers;
using Wikiled.Common.Logging;
using Wikiled.Redis.Indexing;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Serialization
{
    public class RedisSet : IRedisSetList
    {
        private static readonly ILogger log = ApplicationLogging.CreateLogger<RedisSet>();

        private readonly IRedisLink link;

        private readonly IMainIndexManager indexManager;

        public RedisSet(IRedisLink link, IMainIndexManager indexManager)
        {
            this.link = link ?? throw new ArgumentNullException(nameof(link));
            this.indexManager = indexManager ?? throw new ArgumentNullException(nameof(indexManager));
        }

        public Task<long> GetLength(IDatabaseAsync database, RedisKey key)
        {
            return database.SortedSetLengthAsync(key);
        }

        public Task<RedisValue[]> GetRedisValues(IDatabaseAsync database, RedisKey key, long from = 0, long to = -1)
        {
            // to and from has a different meaning here
            if (from != 0 &&
                to != -1)
            {
                return database.SortedSetRangeByScoreAsync(key, from, to, order: Order.Descending);
            }

            return database.SortedSetRangeByScoreAsync(key, order: Order.Descending);
        }

        public Task SaveItems(IDatabaseAsync database, IDataKey key, params RedisValue[] redisValues)
        {
            var redisKey = link.GetKey(key);
            log.LogDebug("AddSet: <{0}>", key);

            var time = DateTime.UtcNow.ToUnixTime();
            var saveTask = database.SortedSetAddAsync(
                redisKey,
                redisValues.Select(item => new SortedSetEntry(item, time)).ToArray());

            List<Task> tasks = new List<Task>(indexManager.Add(database, key));
            tasks.Add(saveTask);
            return Task.WhenAll(tasks);
        }
    }
}
