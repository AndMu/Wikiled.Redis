using System;
using System.Threading.Tasks;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Persistency
{
    public interface IEntityRepository<T> : IBasicRepository<T>
        where T : class, new()
    {
        IObservable<(IDataKey Key, string Command, T Intance)> SubscribeToChanges();

        Task Save(T entity, IRedisTransaction transaction, params IIndexKey[] indexes);

        Task Save(T entity, IIndexKey[] indexes);

        Task<long> Count(IIndexKey key);

        IObservable<T> LoadAll(IIndexKey key);

        Task<T[]> LoadPage(IIndexKey key, int start = 0, int end = -1);

        Task Delete(string id, IRedisTransaction transaction = null, params IIndexKey[] indexes);
    }
}