using System;
using StackExchange.Redis;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Core.Utility.Serialization;

namespace Wikiled.Redis.Data
{
    public class BinaryDataSerializer : IDataSerializer
    {
        public RedisValue Serialize<T>(T instance) 
        {
            Guard.NotNull(() => instance, instance);
            return instance.GetArrayBin();
        }

        public T Deserialize<T>(RedisValue data) 
        {
            Guard.NotNull(() => data, data);
            byte[] array = data;
            return array.GetObjectBin<T>();
        }

        public object Deserialize(Type type, byte[] data)
        {
            Guard.NotNull(() => data, data);
            return data.GetObjectBin();
        }
    }
}
