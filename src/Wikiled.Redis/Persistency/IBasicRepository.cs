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

        Task Save(T entity, params IIndexKey[] indexes);

        Task<long> Count(IIndexKey key);

        Task<T[]> LoadPage(IIndexKey key, int start = 0, int end = -1);
    }
}
