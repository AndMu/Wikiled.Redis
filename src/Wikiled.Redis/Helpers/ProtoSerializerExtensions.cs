using System;
using System.IO;
using ProtoBuf;
using Snappy;

namespace Wikiled.Redis.Helpers
{
    public static class ProtoSerializerExtensions
    {
        public static T ProtoDecompressDeserialize<T>(this byte[] data) where T : class
        {
            if (data == null)
            {
                return null;
            }

            using (MemoryStream memoryStream = new MemoryStream(SnappyCodec.Uncompress(data)))
            {
                return Serializer.Deserialize<T>(memoryStream);
            }
        }

        public static object ProtoDecompressDeserialize(this byte[] data, Type type) 
        {
            if (data == null)
            {
                return null;
            }

            using (MemoryStream memoryStream = new MemoryStream(SnappyCodec.Uncompress(data)))
            {
                return Serializer.NonGeneric.Deserialize(type, memoryStream);
            }
        }

        public static T ProtoDeserialize<T>(this byte[] data) where T : class
        {
            if (data == null)
            {
                return null;
            }

            using (var memoryStream = new MemoryStream(data))
            {
                return Serializer.Deserialize<T>(memoryStream);
            }
        }

        public static object ProtoDeserialize(this byte[] data, Type type)
        {
            if (data == null)
            {
                return null;
            }

            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                return Serializer.NonGeneric.Deserialize(type, memoryStream);
            }
        }

        public static byte[] ProtoSerialize<T>(this T instance) where T : class
        {
            return SerializeWithCompression(instance, data => data);
        }

        public static byte[] ProtoSerializeCompress<T>(this T instance) where T : class
        {
            return SerializeWithCompression(instance, SnappyCodec.Compress);
        }

        /// <summary>
        /// Compress only if data is larger than selected threshold
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <param name="compressed"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public static byte[] SmartSerializeCompress<T>(this T instance, out bool compressed, int threshold = 512) 
        {
            bool wasCompressed = false;
            var data = SerializeWithCompression(
                instance,
                bytes =>
                    {
                        if (bytes.Length < threshold)
                        {
                            return bytes;
                        }

                        wasCompressed = true;
                        return SnappyCodec.Compress(bytes);
                    });
            compressed = wasCompressed;
            return data;
        }

        private static byte[] SerializeWithCompression<T>(this T instance, Func<byte[], byte[]> compress) 
        {
            if (instance == null)
            {
                return null;
            }

            using (var memoryStream = new MemoryStream())
            {
                Serializer.Serialize(memoryStream, instance);
                byte[] objectDataAsStream = compress(memoryStream.ToArray());
                return objectDataAsStream;
            }
        }
    }
}