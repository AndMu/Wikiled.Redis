using System;
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
    public class SingleItemSerialization : ISpecificPersistency
    {
        private readonly IRedisLink link;

        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly IObjectSerialization objectSerialization;

        public SingleItemSerialization(IRedisLink link, IObjectSerialization objectSerialization)
        {
            Guard.NotNull(() => link, link);
            Guard.NotNull(() => objectSerialization, objectSerialization);
            this.objectSerialization = objectSerialization;
            this.link = link;
        }

        public Task AddRecord<T>(IDatabaseAsync database, IDataKey objectKey, params T[] instances)
        {
            Guard.NotNull(() => database, database);
            Guard.NotNull(() => objectKey, objectKey);
            Guard.NotNull(() => instances, instances);
            log.Debug("AddRecords: {0}", instances.Length);
            if(instances.Length > 1)
            {
                throw new ArgumentOutOfRangeException();
            }

            var instance = instances[0];
            List<Task> tasks = new List<Task>();
            var actualKey = link.GetKey(objectKey);
            var entries = objectSerialization.GetEntries(instance).ToArray();
            tasks.Add(database.HashSetAsync(actualKey, entries));
            tasks.AddRange(link.Indexing(database, objectKey));
            return Task.WhenAll(tasks);
        }

        public Task AddRecords<T>(IDatabaseAsync database, IEnumerable<IDataKey> keys, params T[] instances)
        {
            Guard.NotNull(() => database, database);
            Guard.NotNull(() => keys, keys);
            Guard.NotNull(() => instances, instances);
            var tasks = keys.Select(dataKey => AddRecord(database, dataKey, instances));
            return Task.WhenAll(tasks);
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
            log.Debug("GetRecords: {0}", key);
            if (fromRecord != 0 &&
                toRecord != -1)
            {
                log.Warn("Selecting index is not supported for single item");
            }

            return Observable.Create<T>(
                async observer =>
                {
                    var exist = await database.KeyExistsAsync(key).ConfigureAwait(false);
                    if (!exist)
                    {
                        log.Debug("Key doesn't exist: {0}", key);
                        observer.OnCompleted();
                        return;
                    }

                    var result = await database.HashGetAllAsync(key).ConfigureAwait(false);
                    var table = result.ToDictionary(item => item.Name, item => item.Value);
                    var columns = objectSerialization.GetColumns<T>();
                    RedisValue[] values = new RedisValue[columns.Length];
                    for (int i = 0; i < columns.Length; i++)
                    {
                        RedisValue value;
                        if (table.TryGetValue(columns[i], out value))
                        {
                            values[i] = value;
                        }
                        else
                        {
                            values[i] = RedisValue.Null;
                        }
                    }

                    var actualValues = objectSerialization.GetInstances<T>(values);
                    foreach (var value in actualValues)
                    {
                        observer.OnNext(value);
                    }

                    observer.OnCompleted();
                });
        }
    }
}
