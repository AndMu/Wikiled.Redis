using System;
using Wikiled.Common.Helpers;
using Wikiled.Common.Serialization;

namespace Wikiled.Redis.Data
{
    public class ZipBinaryDataSerializer : IDataSerializer
    {
        public byte[] Serialize<T>(T instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            return instance.GetArrayBin().Zip();
        }

        public T Deserialize<T>(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return data.UnZip().GetObjectBin<T>();
        }

        public object Deserialize(Type type, byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return data.UnZip().GetObjectBin();
        }
    }
}
