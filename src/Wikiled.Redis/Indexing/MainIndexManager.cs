using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            foreach (var indexManager in GetManagers(dataKey))
            {
                tasks.Add(indexManager.Manager.AddIndex(database, dataKey, indexManager.Index));
            }

            return tasks.ToArray();
        }

        public Task[] Delete(IDatabaseAsync database, IDataKey dataKey)
        {
            var tasks = new List<Task>(dataKey.Indexes.Length);
            foreach (var indexManager in GetManagers(dataKey))
            {
                tasks.Add(indexManager.Manager.RemoveIndex(database, dataKey, indexManager.Index));
            }

            return tasks.ToArray();
        }

        public IIndexManager GetManager(IIndexKey index)
        {
            return factory.Create(index);
        }

        private IEnumerable<(IIndexManager Manager, IIndexKey Index)> GetManagers(IDataKey dataKey)
        {
            if (dataKey == null)
            {
                throw new ArgumentNullException(nameof(dataKey));
            }

            foreach (var index in dataKey.Indexes)
            {
                yield return (GetManager(index), index);
            }
        }
    }
}
