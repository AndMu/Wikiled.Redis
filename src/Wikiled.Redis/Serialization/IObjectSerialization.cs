using System.Collections.Generic;
using StackExchange.Redis;

namespace Wikiled.Redis.Serialization
{
    public interface IObjectSerialization
    {
        string[] GetColumns<T>();

        IEnumerable<HashEntry> GetEntries<T>(T instance);

        IEnumerable<T> GetInstances<T>(RedisValue[] values);
    }
}
