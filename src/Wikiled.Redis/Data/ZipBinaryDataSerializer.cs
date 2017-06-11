using System;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Core.Utility.IO;
using Wikiled.Core.Utility.Serialization;

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
