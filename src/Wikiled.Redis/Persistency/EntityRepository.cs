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

        protected EntityKey Entity { get; }

        protected ILogger<EntityRepository<T>> Log { get; }

        protected IRedisLink Redis { get; }

        public async Task Save(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var id = GetRecordId(entity);
            Log.LogDebug("Saving: {0}", id);

            var key = Entity.GetKey(id);
            key.AddIndex(Entity.AllIndex);

            await CheckExisting(key).ConfigureAwait(false);

            await Redis.Client.AddRecord(key, entity).ConfigureAwait(false);
        }

        public Task<long> Count()
        {
            return Redis.Client.Count(Entity.AllIndex);
        }

        public async Task<T[]> LoadPage(int start = 0, int end = 1)
        {
            return await Redis.Client.GetRecords<T>(Entity.AllIndex, start, end).ToArray();
        }

        public IObservable<T> LoadAll()
        {
            return Redis.Client.GetRecords<T>(Entity.AllIndex);
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

        protected abstract string GetRecordId(T instance);

        protected virtual async Task CheckExisting(IDataKey key)
        {
            var contains = await Redis.Client.ContainsRecord<T>(key).ConfigureAwait(false);
            if (contains)
            {
                await Redis.Client.DeleteAll<T>(key).ConfigureAwait(false);
            }
        }
    }
}
