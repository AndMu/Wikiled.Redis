using System;
using StackExchange.Redis;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Core.Utility.IO;
using Wikiled.Core.Utility.Serialization;

namespace Wikiled.Redis.Data
{
    public class ZipBinaryDataSerializer : IDataSerializer
    {
        public RedisValue Serialize<T>(T instance)
        {
            Guard.NotNull(() => instance, instance);
            return instance.GetArrayBin().Zip();
        }

        public T Deserialize<T>(RedisValue value)
        {
            Guard.NotNull(() => value, value);
            byte[] data = value;
            return data.UnZip().GetObjectBin<T>();
        }

        public object Deserialize(Type type, byte[] data)
        {
            Guard.NotNull(() => data, data);
            return data.UnZip().GetObjectBin();
        }
    }
}
