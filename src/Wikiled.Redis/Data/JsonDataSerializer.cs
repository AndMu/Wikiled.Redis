using System;
using Wikiled.Common.Utilities.Serialization;

namespace Wikiled.Redis.Data
{
    public class JsonDataSerializer : IDataSerializer
    {
        private readonly IJsonSerializer jsonSerializer;

        public JsonDataSerializer(IJsonSerializer jsonSerializer)
        {
            this.jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
        }

        public byte[] Serialize<T>(T instance)
        {
            return jsonSerializer.SerializeArray(instance);
        }

        public T Deserialize<T>(byte[] data)
        {
            return jsonSerializer.Deserialize<T>(data);
        }

        public object Deserialize(Type type, byte[] data)
        {
            return jsonSerializer.Deserialize(data, type);
        }
    }
}
