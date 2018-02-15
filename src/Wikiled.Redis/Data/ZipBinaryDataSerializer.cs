using System;
using Wikiled.Common.Arguments;
using Wikiled.Common.Helpers;
using Wikiled.Common.Serialization;

namespace Wikiled.Redis.Data
{
    public class ZipBinaryDataSerializer : IDataSerializer
    {
        public byte[] Serialize<T>(T instance)
        {
            Guard.NotNull(() => instance, instance);
            return instance.GetArrayBin().Zip();
        }

        public T Deserialize<T>(byte[] data)
        {
            Guard.NotNull(() => data, data);
            return data.UnZip().GetObjectBin<T>();
        }

        public object Deserialize(Type type, byte[] data)
        {
            Guard.NotNull(() => data, data);
            return data.UnZip().GetObjectBin();
        }
    }
}
