using System;
using System.IO;
using System.Net;
using System.Xml;
using System.Xml.Serialization;
using Wikiled.Redis.Config;
using NUnit.Framework;

namespace Wikiled.Redis.UnitTests.Config
{
    [TestFixture]
    public class RedisConfigurationTests
    {
        private RedisConfiguration configuration;

        [SetUp]
        public void Setup()
        {
            configuration = new RedisConfiguration("Wikiled");
        }

        [Test]
        public void DefaultValues()
        {
            Assert.IsTrue(configuration.AllowAdmin);
            Assert.AreEqual(5000, configuration.ConnectTimeout);
            Assert.AreEqual(60, configuration.KeepAlive);
            Assert.AreEqual("Wikiled", configuration.ServiceName);
            Assert.AreEqual(5000, configuration.SyncTimeout);
        }

        [Test]
        public void Construct()
        {
            Assert.Throws<ArgumentNullException>(() => new RedisConfiguration(null));
            var instance = new RedisConfiguration("Wikiled", "localhost", 100);
            Assert.AreEqual("localhost", instance.Endpoints[0].Host);

            instance = new RedisConfiguration("localhost");
            Assert.AreEqual("localhost", instance.Endpoints[0].Host);
        }

        [Test]
        public void GetOptions()
        {
            configuration.Endpoints = new[] { new RedisEndpoint() };
            configuration.Endpoints[0].Host = "localhost";
            configuration.Endpoints[0].Port = 1000;
            var options = configuration.GetOptions();
            Assert.AreEqual(1, options.EndPoints.Count);
            Assert.AreEqual(5000, options.ConnectTimeout);
            Assert.AreEqual(60, options.KeepAlive);
            Assert.AreEqual(5000, options.SyncTimeout);
            Assert.AreEqual("Wikiled", options.ServiceName);
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

            Assert.AreEqual(8000, options.ConnectTimeout);
            Assert.AreEqual(9000, options.SyncTimeout);

            Assert.AreEqual("Host1", ((DnsEndPoint)options.EndPoints[0]).Host);
            Assert.AreEqual(123, ((DnsEndPoint)options.EndPoints[0]).Port);

            Assert.AreEqual("Host2", ((DnsEndPoint)options.EndPoints[1]).Host);
            Assert.AreEqual(124, ((DnsEndPoint)options.EndPoints[1]).Port);
        }
    }
}
