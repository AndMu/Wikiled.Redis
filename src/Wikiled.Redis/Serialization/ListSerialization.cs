using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NLog;
using StackExchange.Redis;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Redis.Data;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Serialization
{
    public class ListSerialization : ISpecificPersistency
    {
        private readonly IRedisLink link;

        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly IRedisSetList redisSetList;

        public ListSerialization(IRedisLink link, IRedisSetList redisSetList)
        {
            Guard.NotNull(() => link, link);
            Guard.NotNull(() => redisSetList, redisSetList);
            this.link = link;
            this.redisSetList = redisSetList;
        }

        public Task AddRecord<T>(IDatabaseAsync database, IDataKey key, params T[] instances)
        {
            Guard.NotNull(() => database, database);
            Guard.NotNull(() => key, key);
            Guard.NotNull(() => instances, instances);
            var redisValues = instances.Select(GetValue).ToArray();
            return redisSetList.SaveItems(database, key, redisValues);
        }

        public Task AddRecords<T>(IDatabaseAsync database, IEnumerable<IDataKey> keys, params T[] instances)
        {
            Guard.NotNull(() => database, database);
            Guard.NotNull(() => keys, keys);
            Guard.NotNull(() => instances, instances);
            var task = keys.Select(dataKey => AddRecord(database, dataKey, instances));
            return Task.WhenAll(task);
        }

        public Task DeleteAll(IDatabaseAsync database, IDataKey key)
        {
            return link.DeleteAll(database, key);
        }

        public IObservable<T> GetRecords<T>(IDatabaseAsync database, IDataKey dataKey, long fromRecord = 0, long toRecord = -1)
        {
            Guard.NotNull(() => database, database);
            Guard.NotNull(() => dataKey, dataKey);
            var key = link.GetKey(dataKey);
            return Observable.Create<T>(
                async observer =>
                {
                    var items =
                        await redisSetList.GetRedisValues(database, key, fromRecord, toRecord).ConfigureAwait(false);
                    var values = GetValues<T>(key, items);
                    foreach (var value in values)
                    {
                        observer.OnNext(value);
                    }

                    observer.OnCompleted();
                });
        }

        private RedisValue GetValue<T>(T instance)
        {
            RedisValue redisValue;
            if (!RedisValueExtractor.TryParsePrimitive(instance, out redisValue))
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
                    log.Debug("{0} Redis value is null", key);
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
                        log.Debug("{0} Data length is zero", key);
                        yield break;
                    }

                    yield return definition.DataSerializer.Deserialize<T>(data);
                }
            }
        }
    }
}
