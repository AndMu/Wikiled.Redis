using System;
using System.Threading.Tasks;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Persistency
{
    public interface IEntityRepository<T> : IBasicRepository<T>
        where T : class, new()
    {
        Task Save(T entity, IRedisTransaction transaction, params IIndexKey[] indexes);

        IObservable<T> LoadAll(IIndexKey key);

        Task Delete(string id, IRedisTransaction transaction = null, params IIndexKey[] indexes);
    }
}