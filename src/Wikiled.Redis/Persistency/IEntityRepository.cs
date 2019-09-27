using System;
using System.Threading.Tasks;
using Wikiled.Redis.Keys;

namespace Wikiled.Redis.Persistency
{
    public interface IEntityRepository<T> : IRepository where T : class, new()
    {
        EntityKey Entity { get; }
         
        Task Save(T entity, params IIndexKey[] indexes);

        Task<long> Count(IIndexKey key);

        Task<T[]> LoadPage(IIndexKey key, int start = 0, int end = 1);

        IObservable<T> LoadAll(IIndexKey key);

        Task<T> LoadSingle(string id);

        Task Delete(string id);
    }
}