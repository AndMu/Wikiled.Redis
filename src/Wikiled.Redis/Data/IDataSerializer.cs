using System;
using StackExchange.Redis;

namespace Wikiled.Redis.Data
{
    public interface IDataSerializer
    {
        RedisValue Serialize<T>(T instance);

        T Deserialize<T>(RedisValue data);

        object Deserialize(Type type, byte[] data);
    }
}
