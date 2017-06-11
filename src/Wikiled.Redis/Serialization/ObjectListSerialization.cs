using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NLog;
using StackExchange.Redis;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Serialization
{
    public class ObjectListSerialization : ISpecificPersistency
    {
        private readonly ConcurrentDictionary<Type, RedisValue[]> columnsCache =
            new ConcurrentDictionary<Type, RedisValue[]>();

        private readonly IRedisLink link;

        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly IObjectSerialization objectSerialization;

        private readonly IRedisSetList redisSetList;

        public ObjectListSerialization(IRedisLink link, IObjectSerialization objectSerialization, IRedisSetList redisSetList)
        {
            Guard.NotNull(() => link, link);
            Guard.NotNull(() => objectSerialization, objectSerialization);
            Guard.NotNull(() => redisSetList, redisSetList);
            this.objectSerialization = objectSerialization;
            this.redisSetList = redisSetList;
            this.link = link;
        }

        public Task AddRecord<T>(IDatabaseAsync database, IDataKey key, params T[] instances)
        {
            Guard.NotNull(() => database, database);
            Guard.NotNull(() => key, key);
            Guard.NotNull(() => instances, instances);
            return AddRecords(database, new[] { key }, instances);
        }

        public Task AddRecords<T>(IDatabaseAsync database, IEnumerable<IDataKey> keys, T[] instances)
        {
            Guard.NotNull(() => database, database);
            Guard.NotNull(() => keys, keys);
            Guard.NotNull(() => instances, instances);
            return Task.WhenAll(instances.Select(item => AddSingleRecord(database, keys, item)));
        }

        public async Task DeleteAll(IDatabaseAsync database, IDataKey key)
        {
            Guard.NotNull(() => database, database);
            Guard.NotNull(() => key, key);
            var actualKey = link.GetKey(key);
            var allKeys = await redisSetList.GetRedisValues(database, actualKey, 0, -1).ConfigureAwait(false);
            var keys = allKeys.Select(item => (RedisKey)item.ToString()).ToList();
            keys.Add(actualKey);
            await database.KeyDeleteAsync(keys.ToArray()).ConfigureAwait(false);
        }

        public string GetKeyPrefix<T>()
        {
            return typeof(T).Name;
        }

        public IObservable<T> GetRecords<T>(IDatabaseAsync database, IDataKey dataKey, long fromRecord = 0, long toRecord = -1)
        {
            Guard.NotNull(() => database, database);
            Guard.NotNull(() => dataKey, dataKey);
            var key = link.GetKey(dataKey);
            log.Debug("GetRecords: {0} {1}:{2}", key, fromRecord, toRecord);
            return Observable.Create<T>(
                async observer =>
                {
                    RedisValue[] columns;
                    if (!columnsCache.TryGetValue(typeof(T), out columns))
                    {
                        var subKey = link.GetKey("object");
                        columns =
                            objectSerialization.GetColumns<T>()
                                               .Select(item => (RedisValue)($"{subKey}:*->" + item))
                                               .ToArray();
                        columnsCache[typeof(T)] = columns;
                    }

                    var exist = await database.KeyExistsAsync(key).ConfigureAwait(false);

                    if (!exist)
                    {
                        log.Debug("Key doesn't exist: {0}", key);
                        observer.OnCompleted();
                        return;
                    }

                    var result =
                        await
                            database.SortAsync(
                                        key,
                                        fromRecord,
                                        toRecord,
                                        Order.Ascending,
                                        SortType.Numeric,
                                        "nosort",
                                        columns).ConfigureAwait(false);
                    if (result.Length % columns.Length != 0)
                    {
                        log.Error(
                            "Result {0} mistmatched with requested number of columns {1}",
                            result.Length,
                            columns.Length);
                        observer.OnCompleted();
                        return;
                    }

                    var values = objectSerialization.GetInstances<T>(result);
                    foreach (var value in values)
                    {
                        observer.OnNext(value);
                    }

                    observer.OnCompleted();
                });
        }

        private Task AddSingleRecord<T>(IDatabaseAsync database, IEnumerable<IDataKey> keys, T instance)
        {
            var definition = link.GetDefinition<T>();
            var id = definition.GetNextId();
            var objectKey = new ObjectKey(GetKeyPrefix<T>(), id);
            var tasks = new List<Task>();
            var actualKey = link.GetKey(objectKey);
            var entries = objectSerialization.GetEntries(instance)
                                             .Where(item => !item.Value.IsNullOrEmpty)
                                             .ToArray();
            var task = database.HashSetAsync(actualKey, entries);
            tasks.Add(task);
            foreach (var currentKey in keys)
            {
                task = redisSetList.SaveItems(database, currentKey, objectKey.RecordId);
                tasks.Add(task);
                tasks.AddRange(link.Indexing(database, currentKey));
            }

            return Task.WhenAll(tasks);
        }
    }
}
