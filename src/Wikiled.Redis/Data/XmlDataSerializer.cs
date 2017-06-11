using System;
using System.Xml.Linq;
using Snappy;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Core.Utility.Extensions;
using Wikiled.Core.Utility.Serialization;

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
            Guard.NotNull(() => instance, instance);
            var data = instance.XmlSerialize().ToString().GetBytes();
            return compressed ? SnappyCodec.Compress(data) : data;
        }

        public T Deserialize<T>(byte[] data) 
        {
            Guard.NotNull(() => data, data);
            data = compressed ? SnappyCodec.Uncompress(data) : data;
            return XDocument.Parse(data.GetString()).XmlDeserialize<T>();
        }

        public object Deserialize(Type type, byte[] data)
        {
            Guard.NotNull(() => data, data);
            data = compressed ? SnappyCodec.Uncompress(data) : data;
            return XDocument.Parse(data.GetString()).XmlDeserialize(type);
        }
    }
}
