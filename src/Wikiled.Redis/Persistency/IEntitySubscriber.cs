using System;

namespace Wikiled.Redis.Persistency
{
    public interface IEntitySubscriber
    {
        IObservable<T> Subscribe<T>(IBasicRepository<T> repository)
            where T : class, new();
    }
}