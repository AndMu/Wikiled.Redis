using System;
using System.Xml.Linq;
using Wikiled.Common.Helpers;
using Wikiled.Common.Serialization;

namespace Wikiled.Redis.Data
{
    public class ZipXmlDataSerializer : IDataSerializer
    {
        public byte[] Serialize<T>(T instance) 
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            return instance.XmlSerializeZip();
        }

        public T Deserialize<T>(byte[] data) 
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return data.XmlDeserializeZip<T>();
        }

        public object Deserialize(Type type, byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return XDocument.Parse(data.UnZipString()).XmlDeserialize(type);
        }
    }
}
