using System;
using System.Threading.Tasks;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Persistency
{
    public interface IBasicRepository<T> : IRepository where T : class, new()
    {
        EntityKey Entity { get; }

        IRedisLink Redis { get; }

        Task<T> LoadSingle(string id);

        Task Save(T entity);

        Task<long> Count();

        IObservable<T> LoadAll();

        Task<T[]> LoadPage(int start = 0, int end = -1);
    }
}
