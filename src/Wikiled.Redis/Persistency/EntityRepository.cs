using Microsoft.Extensions.Logging;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Persistency
{
    public abstract class EntityRepository<T> : IEntityRepository<T> 
        where T : class, new()
    {
        protected EntityRepository(ILogger<EntityRepository<T>> log, IRedisLink redis, string entity)
        {
            Log = log ?? throw new ArgumentNullException(nameof(log));
            Redis = redis ?? throw new ArgumentNullException(nameof(redis));
            redis.RegisterHashType<T>().IsSingleInstance = true;
            Name = $"{entity}s";
            Entity = new EntityKey(entity, this);
        }

        public string Name { get; }

        public EntityKey Entity { get; }

        protected ILogger<EntityRepository<T>> Log { get; }

        protected IRedisLink Redis { get; }
        
        public Task<long> Count(IIndexKey key)
        {
            return Redis.Client.Count(key);
        }

        public async Task<T[]> LoadPage(IIndexKey key, int start = 0, int end = 1)
        {
            return await Redis.Client.GetRecords<T>(key, start, end).ToArray();
        }

        public Task Save(T entity, params IIndexKey[] indexes)
        {
            return SaveInternal(entity, null, indexes);
        }

        public Task Save(T entity, IRedisTransaction transaction, params IIndexKey[] indexes)
        {
            return SaveInternal(entity, transaction, indexes);
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

        public async Task Delete(string id)
        {
            var contains = await Redis.Client.ContainsRecord<T>(Entity.GetKey(id)).ConfigureAwait(false);
            if (contains)
            {
                await Redis.Client.DeleteAll<T>(Entity.GetKey(id)).ConfigureAwait(false);
            }
        }

        protected abstract string GetRecordId(T instance);

        protected abstract Task BeforeSaving(IRedisTransaction transaction, IDataKey key, T entity);

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
            Log.LogDebug("Saving: {0}", id);

            var key = Entity.GetKey(id);
            key.AddIndex(Entity.AllIndex);
            foreach (var index in indexes)
            {
                key.AddIndex(index);
            }

            var transaction = sharedTransaction ?? Redis.StartTransaction();
            var beforeTask = BeforeSaving(transaction, key, entity);

            var addTask = transaction.Client.AddRecord(key, entity);

            if (sharedTransaction == null)
            {
                await transaction.Commit().ConfigureAwait(false);
                await Task.WhenAll(beforeTask, addTask).ConfigureAwait(false);
            }
        }
    }
}
