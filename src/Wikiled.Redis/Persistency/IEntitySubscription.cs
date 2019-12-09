using System;
using Wikiled.Redis.Keys;

namespace Wikiled.Redis.Persistency
{
    public interface IEntitySubscription<T>
        where T : class, new()
    {
        IObservable<(IDataKey Key, string Command, T Instance)> CreateSubscription();
    }
}