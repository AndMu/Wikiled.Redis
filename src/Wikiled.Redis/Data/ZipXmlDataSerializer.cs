using System;
using System.Xml.Linq;
using StackExchange.Redis;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Core.Utility.IO;
using Wikiled.Core.Utility.Serialization;

namespace Wikiled.Redis.Data
{
    public class ZipXmlDataSerializer : IDataSerializer
    {
        public RedisValue Serialize<T>(T instance) 
        {
            Guard.NotNull(() => instance, instance);
            return instance.XmlSerializeZip();
        }

        public T Deserialize<T>(RedisValue value) 
        {
            Guard.NotNull(() => value, value);
            byte[] data = value;
            return data.XmlDeserializeZip<T>();
        }

        public object Deserialize(Type type, byte[] data)
        {
            Guard.NotNull(() => data, data);
            return XDocument.Parse(data.UnZipString()).XmlDeserialize(type);
        }
    }
}
