using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wikiled.Redis.Indexing;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Persistency;
using IDataKey = Wikiled.Redis.Keys.IDataKey;

namespace Wikiled.Redis.Serialization
{
    public class RedisList : IRedisSetList
    {
        private readonly IRedisLink link;

        private readonly IMainIndexManager mainIndexManager;

        private readonly ILogger<RedisList> logger;

        public RedisList(ILogger<RedisList> logger, IRedisLink link, IMainIndexManager mainIndexManager)
        {
            this.link = link ?? throw new ArgumentNullException(nameof(link));
            this.mainIndexManager = mainIndexManager ?? throw new ArgumentNullException(nameof(mainIndexManager));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<long> GetLength(IDatabaseAsync database, RedisKey key)
        {
            return database.ListLengthAsync(key);
        }

        public Task<RedisValue[]> GetRedisValues(IDatabaseAsync database, RedisKey key, long from, long to)
        {
            return database.ListRangeAsync(key, from, to);
        }

        public Task SaveItems(IDatabaseAsync database, IDataKey key, params RedisValue[] redisValues)
        {
            var redisKey = link.GetKey(key);
            logger.LogTrace("SaveItems: <{0}>", key);

            var tasks = new List<Task>(mainIndexManager.Add(database, key));
            var size = GetLimit(key);
            if (size.HasValue)
            {
                var list = redisValues.ToList();
                list.Add(size.Value);
                tasks.Add(database.ScriptEvaluateAsync(
                    link.Generator.GenerateInsertScript(true, redisValues.Length),
                    new[] { redisKey },
                    list.ToArray()));
            }
            else
            {
                tasks.Add(database.ListRightPushAsync(redisKey, redisValues));
            }
            
            return Task.WhenAll(tasks);
        }

        private long? GetLimit(IDataKey key)
        {
            var repository = key as RepositoryKey;
            var limitedSizeRepository = repository?.Repository as ILimitedSizeRepository;
            return limitedSizeRepository?.Size;
        }
    }
}
