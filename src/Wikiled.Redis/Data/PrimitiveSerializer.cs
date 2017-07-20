using System;
using StackExchange.Redis;

namespace Wikiled.Redis.Data
{
    public class PrimitiveSerializer : IDataSerializer
    {
        public RedisValue Serialize<T>(T instance)
        {
            var result = instance as PrimitiveSet;
            if (result == null)
            {
                throw new ArgumentOutOfRangeException(nameof(instance));
            }

            return result.Value;
        }

        public T Deserialize<T>(RedisValue data)
        {
            return (T)(object)new PrimitiveSet(data);
        }

        public object Deserialize(Type type, byte[] data)
        {
            throw new NotSupportedException();
        }
    }
}
