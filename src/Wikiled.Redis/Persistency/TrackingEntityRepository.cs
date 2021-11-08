using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using StackExchange.Redis;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Persistency
{
    public abstract class TrackingEntityRepository<T> : EntityRepository<T>, ITrackingEntityRepository<T>
        where T : class, new()
    {
        private readonly Lazy<IIndexKey> active;

        private readonly Lazy<IIndexKey> inactive;

        protected TrackingEntityRepository(ILogger<EntityRepository<T>> log, IRedisLink redis, string entity, bool extended = false) 
            : base(log, redis, entity, extended)
        {
            active = new Lazy<IIndexKey>(() => Entity.GenerateIndex("Active"));
            inactive = new Lazy<IIndexKey>(() => Entity.GenerateIndex("Inactive"));
        }

        public IIndexKey Active => active.Value;

        public IIndexKey InActive => inactive.Value;

        public Task Deactivate(string id, IRedisTransaction transaction = null)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            var key = Entity.GetKey(id);
            var client = (IDatabase)transaction?.Transaction ?? Redis.Multiplexer.Database;
            var activeTask = Redis.IndexManager.GetManager(Active).RemoveIndex(client, key, Active);
            var inactiveTask = Redis.IndexManager.GetManager(InActive).AddIndex(client, key, InActive);
            return Task.WhenAll(activeTask, inactiveTask);
        }

        public IObservable<T> LoadActive()
        {
            return LoadAll(Active);
        }

        public IObservable<T> LoadInActive()
        {
            return LoadAll(InActive);
        }

        protected override Task BeforeSaving(IRedisTransaction transaction, IDataKey key, T entity)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            key.AddIndex(Active);
            return base.BeforeSaving(transaction, key, entity);
        }
    }
}
