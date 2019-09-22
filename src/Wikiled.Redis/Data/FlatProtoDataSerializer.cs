using System;
using System.IO;
using Microsoft.IO;
using Wikiled.Common.Reflection;
using Wikiled.FlatBuffers;
using Wikiled.Redis.Helpers;

namespace Wikiled.Redis.Data
{
    public class FlatProtoDataSerializer : IDataSerializer
    {
        private readonly bool isWellKnown;

        private readonly RecyclableMemoryStreamManager memoryStreamManager;

        public FlatProtoDataSerializer(bool isWellKnown, RecyclableMemoryStreamManager memoryStreamManager)
        {
            this.isWellKnown = isWellKnown;
            this.memoryStreamManager = memoryStreamManager ?? throw new ArgumentNullException(nameof(memoryStreamManager));
        }

        public T Deserialize<T>(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

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
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var redisData = RedisData.GetRootAsRedisData(new ByteBuffer(data));
            if (!string.IsNullOrEmpty(redisData.Type))
            {
                type = redisData.Type.ResolveType();
            }

            return DeserializeInternal(type, redisData);
        }

        public byte[] Serialize<T>(T instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            var data = instance.SmartSerializeCompress(out bool compressed);
            var builder = new FlatBufferBuilder(data.Length + 255);

            // allocate payload
            var payload = RedisData.CreatePayloadVector(builder, data);
            var typeId = GetTypeId(instance, builder);
            RedisData.StartRedisData(builder);
            RedisData.AddCompressed(builder, compressed);
            RedisData.AddType(builder, typeId);
            RedisData.AddPayload(builder, payload);
            int message = RedisData.EndRedisData(builder);
            RedisData.FinishRedisDataBuffer(builder, message);

            using (var memoryStream = memoryStreamManager.GetStream("Redis.FlatBuffers", builder.DataBuffer.Data, builder.DataBuffer.Position, builder.Offset))
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
