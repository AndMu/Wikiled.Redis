using System;
using System.Xml.Linq;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Core.Utility.IO;
using Wikiled.Core.Utility.Serialization;

namespace Wikiled.Redis.Data
{
    public class ZipXmlDataSerializer : IDataSerializer
    {
        public byte[] Serialize<T>(T instance) 
        {
            Guard.NotNull(() => instance, instance);
            return instance.XmlSerializeZip();
        }

        public T Deserialize<T>(byte[] data) 
        {
            Guard.NotNull(() => data, data);
            return data.XmlDeserializeZip<T>();
        }

        public object Deserialize(Type type, byte[] data)
        {
            Guard.NotNull(() => data, data);
            return XDocument.Parse(data.UnZipString()).XmlDeserialize(type);
        }
    }
}
