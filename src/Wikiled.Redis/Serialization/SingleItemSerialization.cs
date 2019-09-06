using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Wikiled.Common.Logging;
using Wikiled.Redis.Indexing;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Serialization
{
    public class SingleItemSerialization : BaseSetSerialization, ISpecificPersistency
    {
        private readonly IRedisLink link;

        private static readonly ILogger log = ApplicationLogging.CreateLogger<SingleItemSerialization>();

        private readonly IObjectSerialization objectSerialization;

        private readonly IMainIndexManager mainIndexManager;

        public SingleItemSerialization(IRedisLink link, IObjectSerialization objectSerialization, IMainIndexManager mainIndexManager)
            : base(link)
        {
            this.objectSerialization = objectSerialization ?? throw new ArgumentNullException(nameof(objectSerialization));
            this.mainIndexManager = mainIndexManager ?? throw new ArgumentNullException(nameof(mainIndexManager));
            this.link = link ?? throw new ArgumentNullException(nameof(link));
        }

        public Task AddRecord<T>(IDatabaseAsync database, IDataKey objectKey, params T[] instances)
        {
            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            if (objectKey == null)
            {
                throw new ArgumentNullException(nameof(objectKey));
            }

            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            log.LogDebug("AddRecords: {0}", instances.Length);
            if (instances.Length > 1)
            {
                throw new ArgumentOutOfRangeException();
            }

            var instance = instances[0];
            var tasks = new List<Task>();
            var actualKey = link.GetKey(objectKey);
            var entries = objectSerialization.GetEntries(instance).ToArray();
            tasks.Add(database.HashSetAsync(actualKey, entries));
            tasks.AddRange(mainIndexManager.Add(database, objectKey));
            return Task.WhenAll(tasks);
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

            var tasks = keys.Select(dataKey => AddRecord(database, dataKey, instances));
            return Task.WhenAll(tasks);
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
            log.LogDebug("GetRecords: {0}", key);
            if (fromRecord != 0 &&
                toRecord != -1)
            {
                log.LogWarning("Selecting index is not supported for single item");
            }

            return Observable.Create<T>(
                async observer =>
                {
                    var exist = await database.KeyExistsAsync(key).ConfigureAwait(false);
                    if (!exist)
                    {
                        log.LogDebug("Key doesn't exist: {0}", key);
                        observer.OnCompleted();
                        return;
                    }

                    var result = await database.HashGetAllAsync(key).ConfigureAwait(false);
                    var actualValues = ContructActualValues<T>(result);
                    foreach (var value in actualValues)
                    {
                        observer.OnNext(value);
                    }

                    observer.OnCompleted();
                });
        }

        private IEnumerable<T> ContructActualValues<T>(HashEntry[] result)
        {
            var values = GetRedisValues<T>(result);
            var actualValues = objectSerialization.GetInstances<T>(values);
            return actualValues;
        }

        private RedisValue[] GetRedisValues<T>(HashEntry[] result)
        {
            var table = result.ToDictionary(item => item.Name, item => item.Value);
            var columns = objectSerialization.GetColumns<T>();
            var values = new RedisValue[columns.Length];
            for (var i = 0; i < columns.Length; i++)
            {
                if (table.TryGetValue(columns[i], out var value))
                {
                    values[i] = value;
                }
                else
                {
                    values[i] = RedisValue.Null;
                }
            }

            return values;
        }
    }
}
