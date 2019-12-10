using System.Collections.Generic;
using StackExchange.Redis;

namespace Wikiled.Redis.Serialization
{
    public interface IObjectSerialization<T>
    {
        string[] GetColumns();

        IEnumerable<HashEntry> GetEntries(T instance);

        IEnumerable<T> GetInstances(RedisValue[] values);
    }
}
