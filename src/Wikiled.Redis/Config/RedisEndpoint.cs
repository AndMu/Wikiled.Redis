using System.Xml.Serialization;

namespace Wikiled.Redis.Config
{
    [XmlRoot("Endpoint")]
    public class RedisEndpoint
    {
        [XmlAttribute("host")]
        public string Host { get; set; }

        [XmlAttribute("port")]
        public int Port { get; set; }

        public override string ToString()
        {
            return $"{Host}:{Port}";
        }
    }
}
