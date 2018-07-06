using System;
using Wikiled.Common.Serialization;

namespace Wikiled.Redis.Data
{
    public class BinaryDataSerializer : IDataSerializer
    {
        public byte[] Serialize<T>(T instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            return instance.GetArrayBin();
        }

        public T Deserialize<T>(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return data.GetObjectBin<T>();
        }

        public object Deserialize(Type type, byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return data.GetObjectBin();
        }
    }
}
