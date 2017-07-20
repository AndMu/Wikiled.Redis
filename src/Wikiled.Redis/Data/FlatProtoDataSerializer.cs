using System;
using System.IO;
using StackExchange.Redis;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Core.Utility.Extensions;
using Wikiled.FlatBuffers;

namespace Wikiled.Redis.Data
{
    public class FlatProtoDataSerializer : IDataSerializer
    {
        private readonly bool isWellKnown;

        public FlatProtoDataSerializer(bool isWellKnown)
        {
            this.isWellKnown = isWellKnown;
        }

        public T Deserialize<T>(RedisValue value)
        {
            Guard.NotNull(() => value, value);
            byte[] data = value;
            var redisData = RedisData.GetRootAsRedisData(new ByteBuffer(data));
            Type type = typeof(T);
            if (!string.IsNullOrEmpty(redisData.Type))
            {
                type = redisData.Type.ResolveType();
            }

            return (T)DeserializeInternal(type, redisData);
        }

        public object Deserialize(Type type, byte[] data)
        {
            Guard.NotNull(() => data, data);
            var redisData = RedisData.GetRootAsRedisData(new ByteBuffer(data));
            if (!string.IsNullOrEmpty(redisData.Type))
            {
                type = redisData.Type.ResolveType();
            }

            return DeserializeInternal(type, redisData);
        }

        public RedisValue Serialize<T>(T instance)
        {
            Guard.NotNull(() => instance, instance);
            bool compressed;
            var data = instance.SmartSerializeCompress(out compressed);
            FlatBufferBuilder builder = new FlatBufferBuilder(data.Length + 255);

            // allocate payload
            var payload = RedisData.CreatePayloadVector(builder, data);
            var typeId = GetTypeId(instance, builder);
            RedisData.StartRedisData(builder);
            RedisData.AddCompressed(builder, compressed);
            RedisData.AddType(builder, typeId);
            RedisData.AddPayload(builder, payload);
            int message = RedisData.EndRedisData(builder);
            RedisData.FinishRedisDataBuffer(builder, message);

            using (var memoryStream = new MemoryStream(builder.DataBuffer.Data, builder.DataBuffer.Position, builder.Offset))
            {
                return memoryStream.ToArray();
            }
        }

        private static object DeserializeInternal(Type type, RedisData redisData)
        {
            byte[] payload = redisData.GetPayload();
            if (redisData.Compressed)
            {
                return payload.ProtoDecompressDeserialize(type);
            }

            return payload.ProtoDeserialize(type);
        }

        private int AllocateString(FlatBufferBuilder builder, string text)
        {
            return string.IsNullOrEmpty(text) ? 0 : builder.CreateString(text);
        }

        private int GetTypeId<T>(T instance, FlatBufferBuilder builder) 
        {
            if (isWellKnown)
            {
                return 0;
            }

            var typeString = instance.GetType().GetTypeName();
            int typeId = AllocateString(builder, typeString);
            return typeId;
        }
    }
}
