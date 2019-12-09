using System;
using Wikiled.Redis.Keys;

namespace Wikiled.Redis.Persistency
{
    public interface IEntitySubscriber
    {
        IObservable<(IDataKey Key, string Command, T Intance)> Subscribe<T>(IBasicRepository<T> repository)
            where T : class, new();
    }
}