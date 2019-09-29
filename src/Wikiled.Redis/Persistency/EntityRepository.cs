using Microsoft.Extensions.Logging;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;
using Wikiled.Redis.Helpers;
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

        public IObservable<T> SubscribeToChanges()
        {
            var key = Entity.GetKey("*");
            return Observable.Create<T>(
                async (observer, token) => { await EntityEventSubscription(key, token, observer); });
        }

        public Task<long> Count(IIndexKey key)
        {
            return Redis.Client.Count(key);
        }

        public async Task<T[]> LoadPage(IIndexKey key, int start = 0, int end = -1)
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
            }

            await Task.WhenAll(beforeTask, addTask).ConfigureAwait(false);
        }

        private async Task EntityEventSubscription(IDataKey key, CancellationToken token, IObserver<T> observer)
        {
            ISubscriber subscriber = null;
            try
            {
                subscriber = Redis.Multiplexer.SubscribeKeyEvents(
                    key.FullKey,
                    async events =>
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }

                        var receivedKey = events.Channel.ToString();
                        var start = receivedKey.IndexOf(FieldConstants.ObjectTag) + 8;
                        if (start < 0)
                        {
                            Log.LogWarning("Bad key: {0}", receivedKey);
                            return;
                        }

                        receivedKey = receivedKey.Substring(start);
                        var keyItem = new RepositoryKey(this, new ObjectKey(receivedKey));
                        var record = await Redis.Client.GetRecords<T>(keyItem).LastOrDefaultAsync();
                        observer.OnNext(record);
                    });

                await token.AsTask();
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                subscriber?.UnsubscribeAll();
            }
        }
    }
}
