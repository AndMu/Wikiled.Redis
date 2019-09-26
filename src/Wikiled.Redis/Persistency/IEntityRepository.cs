using System;
using System.Threading.Tasks;

namespace Wikiled.Redis.Persistency
{
    public interface IEntityRepository<T> : IRepository where T : class, new()
    {
        string Name { get; }

        Task Save(T entity);

        Task<long> Count();

        Task<T[]> LoadPage(int start = 0, int end = 1);

        IObservable<T> LoadAll();

        Task<T> LoadSingle(string id);
    }
}