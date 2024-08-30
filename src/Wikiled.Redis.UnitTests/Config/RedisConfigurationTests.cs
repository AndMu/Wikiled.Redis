using System;
using System.IO;
using System.Net;
using System.Xml;
using System.Xml.Serialization;
using Wikiled.Redis.Config;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Wikiled.Redis.UnitTests.Config
{
    [TestFixture]
    public class RedisConfigurationTests
    {
        private RedisConfiguration configuration;

        [SetUp]
        public void Setup()
        {
            configuration = new RedisConfiguration();
        }

        [Test]
        public void DefaultValues()
        {
            ClassicAssert.IsTrue(configuration.AllowAdmin);
            ClassicAssert.AreEqual(5000, configuration.ConnectTimeout);
            ClassicAssert.AreEqual(60, configuration.KeepAlive);
            ClassicAssert.AreEqual("Wikiled", configuration.ServiceName);
            ClassicAssert.AreEqual(5000, configuration.SyncTimeout);
        }

        [Test]
        public void Construct()
        {
            ClassicAssert.Throws<ArgumentNullException>(() => new RedisConfiguration(null));
            var instance = new RedisConfiguration("localhost", 100);
            ClassicAssert.AreEqual("localhost", instance.Endpoints[0].Host);

            instance = new RedisConfiguration("localhost");
            ClassicAssert.AreEqual("localhost", instance.Endpoints[0].Host);
        }

        [Test]
        public void GetOptions()
        {
            configuration.Endpoints = new[] { new RedisEndpoint() };
            configuration.Endpoints[0].Host = "localhost";
            configuration.Endpoints[0].Port = 1000;
            var options = configuration.GetOptions();
            ClassicAssert.AreEqual(1, options.EndPoints.Count);
            ClassicAssert.AreEqual(5000, options.ConnectTimeout);
            ClassicAssert.AreEqual(60, options.KeepAlive);
            ClassicAssert.AreEqual(5000, options.SyncTimeout);
        }

        [Test]
        public void String()
        {
            configuration.Endpoints = new[] { new RedisEndpoint() };
            configuration.Endpoints[0].Host = "localhost";
            configuration.Endpoints[0].Port = 1000;
            configuration.ServiceName = "Test";
            ClassicAssert.AreEqual("[Test] [localhost:1000]", configuration.ToString());
        }

        [Test]
        public void DeserializeFromString()
        {
            string xml = @"<RedisConfig>
  <ConnectRetry>3</ConnectRetry>
  <Endpoints>
    <Endpoint host='Host1' port='123' />
    <Endpoint host='Host2' port='124' />
  </Endpoints>
  <ConnectTimeout>8000</ConnectTimeout>
  <SyncTimeout>9000</SyncTimeout>
</RedisConfig>";

            var serializer = new XmlSerializer(typeof(RedisConfiguration));
            var redisSettings = (RedisConfiguration)serializer.Deserialize(XmlReader.Create(new StringReader(xml)));

            var options = redisSettings.GetOptions();

            ClassicAssert.AreEqual(8000, options.ConnectTimeout);
            ClassicAssert.AreEqual(9000, options.SyncTimeout);

            ClassicAssert.AreEqual("Host1", ((DnsEndPoint)options.EndPoints[0]).Host);
            ClassicAssert.AreEqual(123, ((DnsEndPoint)options.EndPoints[0]).Port);

            ClassicAssert.AreEqual("Host2", ((DnsEndPoint)options.EndPoints[1]).Host);
            ClassicAssert.AreEqual(124, ((DnsEndPoint)options.EndPoints[1]).Port);
        }
    }
}
