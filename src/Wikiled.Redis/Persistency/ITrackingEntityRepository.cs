using System;
using System.Threading.Tasks;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Persistency
{
    public interface ITrackingEntityRepository<T> : IEntityRepository<T>
        where T : class, new()
    {
        IIndexKey Active { get; }

        IIndexKey InActive { get; }

        Task Deactivate(string id, IRedisTransaction transaction);

        IObservable<T> LoadActive();

        IObservable<T> LoadInActive();
    }
}
