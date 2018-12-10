﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Wikiled.Common.Logging;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Persistency;

namespace Wikiled.Redis.Serialization
{
    public class RedisList : IRedisSetList
    {
        private static readonly ILogger log = ApplicationLogging.CreateLogger<RedisList>();

        private readonly IRedisLink link;

        public RedisList(IRedisLink link)
        {
            this.link = link ?? throw new ArgumentNullException(nameof(link));
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
            log.LogDebug("AddSet: <{0}>", key);

            var size = GetLimit(key);
            if(size.HasValue)
            {
                var list = redisValues.ToList();
                list.Add(size.Value);
                return database.ScriptEvaluateAsync(
                    link.Generator.GenerateInsertScript(true, redisValues.Length),
                    new[] {redisKey},
                    list.ToArray());
            }

            List<Task> tasks = new List<Task>(link.Indexing(database, key));
            tasks.Add(database.ListRightPushAsync(redisKey, redisValues));
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
