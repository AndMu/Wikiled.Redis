using System;

namespace Wikiled.Redis.Data
{
    public interface IDataSerializer
    {
        byte[] Serialize<T>(T instance);

        T Deserialize<T>(byte[] data);

        object Deserialize(Type type, byte[] data);
    }
}
