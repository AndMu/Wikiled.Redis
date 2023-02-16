using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Persistency
{
    public abstract class EntityRepository<T> : IEntityRepository<T>
        where T : class, new()
    {
        private IObservable<(IDataKey Key, string Command, T Intance)> subscription;

        protected EntityRepository(ILogger<EntityRepository<T>> log, IRedisLink redis, string entity, bool extended = false, Action<IPersistencyRegistrationHandler> register = null)
        {
            Log = log ?? throw new ArgumentNullException(nameof(log));
            Redis = redis ?? throw new ArgumentNullException(nameof(redis));
            if (register != null)
            {
                register(redis.PersistencyRegistration);
            }
            else
            {
                redis.PersistencyRegistration.RegisterHashsetSingle<T>();
            }
            
            Name = $"{entity}Repo";
            Entity = new EntityKey(extended ? entity : string.Empty, this);
        }

        public string Name { get; }

        public EntityKey Entity { get; }

        protected ILogger<EntityRepository<T>> Log { get; }

        public IRedisLink Redis { get; }

        public IObservable<(IDataKey Key, string Command, T Intance)> SubscribeToChanges()
        {
            subscription ??= Redis.EntitySubscriber.Subscribe(this);
            return subscription;
        }

        public Task<long> Count(IIndexKey key)
        {
            return Redis.Client.Count(key);
        }

        public Task<long> Count(IDataKey key)
        {
            return Redis.Client.Count<T>(key);
        }

        public Task<long> Count()
        {
            return Count(Entity.AllIndex);
        }

        public IObservable<T> LoadAll()
        {
            return LoadAll(Entity.AllIndex);
        }

        public async Task<T[]> LoadPage(IIndexKey key, int start = 0, int end = -1)
        {
            return await Redis.Client.GetRecords<T>(key, start, end).ToArray();
        }

        public Task<T[]> LoadPage(int start = 0, int end = -1)
        {
            return LoadPage(Entity.AllIndex, start, end);
        }

        public Task Save(T entity, params IIndexKey[] indexes)
        {
            return SaveInternal(entity, null, indexes);
        }

        public Task Save(T entity)
        {
            return SaveInternal(entity, null, Array.Empty<IIndexKey>());
        }

        public Task Save(T entity, IRedisTransaction transaction, params IIndexKey[] indexes)
        {
            return SaveInternal(entity, transaction, indexes);
        }

        public IObservable<T> LoadAll(IDataKey key)
        {
            return Redis.Client.GetRecords<T>(key);
        }

        public IObservable<T> LoadAll(IIndexKey key)
        {
            return Redis.Client.GetRecords<T>(key);
        }

        public async Task<T> LoadSingle(string id)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            var key = Entity.GetKey(id);
            return await Redis.Client.GetRecords<T>(key).LastOrDefaultAsync();
        }

        public async Task Delete(string id, IRedisTransaction transaction = null, params IIndexKey[] indexes)
        {
            var client = transaction?.Client ?? Redis.Client;
            var entity = Entity.GetKey(id);
            entity.AddIndex(Entity.AllIndex);
            foreach (var index in indexes)
            {
                entity.AddIndex(index);
            }

            await client.DeleteAll<T>(entity).ConfigureAwait(false);
        }

        protected abstract string GetRecordId(T instance);

        protected virtual Task BeforeSaving(IRedisTransaction transaction, IDataKey key, T entity)
        {
            return Task.CompletedTask;
        }

        protected virtual Task AfterSaving(IDataKey key, T entity)
        {
            return Task.CompletedTask;
        }

        protected async Task SaveInternal(T entity, IRedisTransaction sharedTransaction = null, params IIndexKey[] indexes)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (indexes == null)
            {
                throw new ArgumentNullException(nameof(indexes));
            }

            var id = GetRecordId(entity);
            Log.LogTrace("Saving: {0}", id);

            var key = Entity.GetKey(id);
            foreach (var index in GetKeys().Union(indexes))
            {
                key.AddIndex(index);
            }

            var transaction = sharedTransaction ?? Redis.StartTransaction();
            var beforeTask = BeforeSaving(transaction, key, entity);

            var addTask = transaction.Client.AddRecord(key, entity);

            if (sharedTransaction == null)
            {
                await transaction.Commit().ConfigureAwait(false);
            }

            await Task.WhenAll(beforeTask, addTask).ConfigureAwait(false);
            Log.LogTrace("AfterSaving");
            await AfterSaving(key, entity).ConfigureAwait(false);
            Log.LogTrace("Saving Completed: {0}", id);
        }

        protected virtual IEnumerable<IIndexKey> GetKeys()
        {
            yield return Entity.AllIndex;
        }
    }
}
