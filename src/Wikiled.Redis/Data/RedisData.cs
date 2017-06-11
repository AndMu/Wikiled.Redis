using Wikiled.FlatBuffers;

namespace Wikiled.Redis.Data
{
    /// <summary>
    ///     automatically generated, do not modify
    /// </summary>
    public sealed class RedisData : Table
    {
        private bool? isCompressed;

        private int? payloadLength;

        private string type;

        public bool Compressed
        {
            get
            {
                if (!isCompressed.HasValue)
                {
                    int o = __offset(6);
                    isCompressed = o != 0 && 0 != bb.Get(o + bb_pos);
                }

                return isCompressed.Value;
            }
        }

        public int PayloadLength
        {
            get
            {
                if (!payloadLength.HasValue)
                {
                    int o = __offset(8);
                    payloadLength = o != 0 ? __vector_len(o) : 0;
                }

                return payloadLength.Value;
            }
        }

        public string Type
        {
            get
            {
                if (type == null)
                {
                    int o = __offset(4);
                    type = o != 0 ? __string(o + bb_pos) : null;
                }

                return type;
            }
        }

        public static void AddCompressed(FlatBufferBuilder builder, bool compressed)
        {
            builder.AddBool(1, compressed, false);
        }

        public static void AddPayload(FlatBufferBuilder builder, int payloadOffset)
        {
            builder.AddOffset(2, payloadOffset, 0);
        }

        public static void AddType(FlatBufferBuilder builder, int typeOffset)
        {
            builder.AddOffset(0, typeOffset, 0);
        }

        public static int CreatePayloadVector(FlatBufferBuilder builder, byte[] data)
        {
            return builder.AddByteArray(data);
        }

        public static int EndRedisData(FlatBufferBuilder builder)
        {
            int o = builder.EndObject();
            return o;
        }

        public static void FinishRedisDataBuffer(FlatBufferBuilder builder, int offset)
        {
            builder.Finish(offset);
        }

        public static RedisData GetRootAsRedisData(ByteBuffer byteBuffer)
        {
            return GetRootAsRedisData(byteBuffer, new RedisData());
        }

        public static RedisData GetRootAsRedisData(ByteBuffer byteBuffer, RedisData obj)
        {
            return obj.Init(byteBuffer.GetInt(byteBuffer.Position) + byteBuffer.Position, byteBuffer);
        }

        public static void StartRedisData(FlatBufferBuilder builder)
        {
            builder.StartObject(3);
        }

        public RedisData Init(int i, ByteBuffer byteBuffer)
        {
            bb_pos = i;
            bb = byteBuffer;
            return this;
        }

        internal byte[] GetPayload()
        {
            int o = __offset(8);
            return o != 0 ? __byteArray(o + bb_pos) : null;
        }
    }
}