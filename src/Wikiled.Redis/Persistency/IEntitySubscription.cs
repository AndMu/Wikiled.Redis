using System;

namespace Wikiled.Redis.Persistency
{
    public interface IEntitySubscription<T>
        where T : class, new()
    {
        IObservable<T> CreateSubscription();
    }
}