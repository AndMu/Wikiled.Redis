using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Wikiled.Common.Logging;
using Wikiled.Redis.Data;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Serialization
{
    public class ListSerialization : BaseSetSerialization, ISpecificPersistency
    {
        private readonly IRedisLink link;

        private static readonly ILogger log = ApplicationLogging.CreateLogger<ListSerialization>();

        private readonly IRedisSetList redisSetList;

        public ListSerialization(IRedisLink link, IRedisSetList redisSetList)
            : base(link)
        {
            this.link = link ?? throw new ArgumentNullException(nameof(link));
            this.redisSetList = redisSetList ?? throw new ArgumentNullException(nameof(redisSetList));
        }

        public Task AddRecord<T>(IDatabaseAsync database, IDataKey key, params T[] instances)
        {
            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            var redisValues = instances.Select(GetValue).ToArray();
            return redisSetList.SaveItems(database, key, redisValues);
        }

        public Task AddRecords<T>(IDatabaseAsync database, IEnumerable<IDataKey> keys, params T[] instances)
        {
            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            var task = keys.Select(dataKey => AddRecord(database, dataKey, instances));
            return Task.WhenAll(task);
        }

        public IObservable<T> GetRecords<T>(IDatabaseAsync database, IDataKey dataKey, long fromRecord = 0, long toRecord = -1)
        {
            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            if (dataKey == null)
            {
                throw new ArgumentNullException(nameof(dataKey));
            }

            var key = link.GetKey(dataKey);
            return Observable.Create<T>(
                async observer =>
                {
                    var items = await link.Resilience.AsyncRetryPolicy.ExecuteAsync(async () => await redisSetList.GetRedisValues(database, key, fromRecord, toRecord).ConfigureAwait(false))
                                          .ConfigureAwait(false);
                    var values = GetValues<T>(key, items);
                    foreach (var value in values)
                    {
                        observer.OnNext(value);
                    }

                    observer.OnCompleted();
                });
        }

        public Task<long> Count(IDatabaseAsync database, IDataKey dataKey)
        {
            var key = link.GetKey(dataKey);
            return redisSetList.GetLength(database, key);
        }

        private RedisValue GetValue<T>(T instance)
        {
            if (!RedisValueExtractor.TryParsePrimitive(instance, out RedisValue redisValue))
            {
                var definition = link.GetDefinition<T>();
                return definition.DataSerializer.Serialize(instance);
            }

            return redisValue;
        }

        private IEnumerable<T> GetValues<T>(RedisKey key, RedisValue[] values)
        {
            var definition = link.GetDefinition<T>();
            foreach (var value in values)
            {
                if (!value.HasValue)
                {
                    log.LogDebug("{0} Redis value is null", key);
                    yield break;
                }

                if (RedisValueExtractor.IsPrimitive<T>())
                {
                    yield return RedisValueExtractor.SafeConvert<T>(value);
                }
                else
                {
                    var data = (byte[])value;
                    if ((data == null) ||
                       (data.Length == 0))
                    {
                        log.LogDebug("{0} Data length is zero", key);
                        yield break;
                    }

                    yield return definition.DataSerializer.Deserialize<T>(data);
                }
            }
        }
    }
}
