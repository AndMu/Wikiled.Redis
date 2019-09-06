using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wikiled.Common.Extensions;
using Wikiled.Redis.Keys;

namespace Wikiled.Redis.Indexing
{
    public class MainIndexManager : IMainIndexManager
    {
        private readonly IIndexManagerFactory factory;

        private readonly ConcurrentDictionary<string, IIndexManager> manager = new ConcurrentDictionary<string, IIndexManager>();

        public MainIndexManager(IIndexManagerFactory factory)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public Task[] Add(IDatabaseAsync database, IDataKey dataKey)
        {
            var tasks = new List<Task>(dataKey.Indexes.Length);
            foreach (var indexManager in GetManagers(database, dataKey))
            {
                tasks.Add(indexManager.AddIndex(dataKey));
            }

            return tasks.ToArray();
        }

        public Task[] Delete(IDatabaseAsync database, IDataKey dataKey)
        {
            var tasks = new List<Task>(dataKey.Indexes.Length);
            foreach (var indexManager in GetManagers(database, dataKey))
            {
                tasks.Add(indexManager.RemoveIndex(dataKey));
            }

            return tasks.ToArray();
        }

        public IIndexManager GetManager(IDatabaseAsync database, params IIndexKey[] index)
        {
            string indexKey = index.Length == 1 ? index[0].Key : index.Select(item => item.Key).AccumulateItems(":");
            return manager.GetOrAdd(indexKey, key => factory.Create(database, index));
        }

        private IEnumerable<IIndexManager> GetManagers(IDatabaseAsync database, IDataKey dataKey)
        {
            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            if (dataKey == null)
            {
                throw new ArgumentNullException(nameof(dataKey));
            }

            foreach (var index in dataKey.Indexes)
            {
                yield return GetManager(database, index);
            }
        }
    }
}
