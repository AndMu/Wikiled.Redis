using System;
using Wikiled.Common.Arguments;
using Wikiled.Common.Serialization;

namespace Wikiled.Redis.Data
{
    public class BinaryDataSerializer : IDataSerializer
    {
        public byte[] Serialize<T>(T instance) 
        {
            Guard.NotNull(() => instance, instance);
            return instance.GetArrayBin();
        }

        public T Deserialize<T>(byte[] data) 
        {
            Guard.NotNull(() => data, data);
            return data.GetObjectBin<T>();
        }

        public object Deserialize(Type type, byte[] data)
        {
            Guard.NotNull(() => data, data);
            return data.GetObjectBin();
        }
    }
}
