using System;
using System.Xml.Linq;
using Snappy;
using Wikiled.Common.Extensions;
using Wikiled.Common.Serialization;

namespace Wikiled.Redis.Data
{
    public class XmlDataSerializer : IDataSerializer
    {
        private readonly bool compressed;

        public XmlDataSerializer(bool compressed = true)
        {
            this.compressed = compressed;
        }

        public byte[] Serialize<T>(T instance) 
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            var data = instance.XmlSerialize().ToString().GetBytes();
            return compressed ? SnappyCodec.Compress(data) : data;
        }

        public T Deserialize<T>(byte[] data) 
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            data = compressed ? SnappyCodec.Uncompress(data) : data;
            return XDocument.Parse(data.GetString()).XmlDeserialize<T>();
        }

        public object Deserialize(Type type, byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            data = compressed ? SnappyCodec.Uncompress(data) : data;
            return XDocument.Parse(data.GetString()).XmlDeserialize(type);
        }
    }
}
