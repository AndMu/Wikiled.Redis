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
    public class SortedSetSerialization : BaseSetSerialization, ISpecificPersistency
    {
        private readonly IRedisLink link;

        private static readonly ILogger log = ApplicationLogging.CreateLogger<SortedSetSerialization>();

        private readonly IMainIndexManager mainIndexManager;

        public SortedSetSerialization(IRedisLink link, IMainIndexManager mainIndexManager)
            : base(link)
        {
            this.link = link ?? throw new ArgumentNullException(nameof(link));
            this.mainIndexManager = mainIndexManager ?? throw new ArgumentNullException(nameof(mainIndexManager));
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

            return AddRecords(database, new[] { key }, instances);
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

            if (typeof(T) != typeof(SortedSetEntry))
            {
                throw new ArgumentOutOfRangeException(nameof(T));
            }

            var tasks = new List<Task>();
            var entries = instances.Cast<SortedSetEntry>().ToArray();
            foreach (var key in keys)
            {
                var redisKey = link.GetKey(key);
                var saveTask = database.SortedSetAddAsync(
                    redisKey,
                    entries);

                tasks.AddRange(mainIndexManager.Add(database, key));
                tasks.Add(saveTask);
            }

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

            if (typeof(T) != typeof(SortedSetEntry))
            {
                throw new ArgumentOutOfRangeException(nameof(T));
            }

            var key = link.GetKey(dataKey);
            return Observable.Create<T>(
                async observer =>
                {
                    var items = await database.SortedSetRangeByScoreWithScoresAsync(key, skip: fromRecord, take: toRecord)
                                              .ConfigureAwait(false);
                    foreach (var value in items)
                    {
                        observer.OnNext((T)(object)value);
                    }

                    observer.OnCompleted();
                });
        }
    }
}
