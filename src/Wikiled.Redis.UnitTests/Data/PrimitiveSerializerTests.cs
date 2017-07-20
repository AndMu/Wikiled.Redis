using System;
using NUnit.Framework;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Data;

namespace Wikiled.Redis.UnitTests.Data
{
    [TestFixture]
    public class PrimitiveSerializerTests
    {
        private PrimitiveSerializer instance;

        [SetUp]
        public void Setup()
        {
            instance = CreatePrimitiveSerializer();
        }

        [TestCase("Test")]
        [TestCase(1)]
        [TestCase(0.5)]
        public void Serialize(object value)
        {
            dynamic dye = new { TestType = "Test"};
            var result = instance.Serialize(value);
            Assert.AreEqual(value.ToString(), result.ToString());
        }

        [Test]
        public void Deserialize()
        {
            var result = instance.Deserialize<string>("Test");
            Assert.AreEqual("Test", result);
        }
        

        [Test]
        public void SerializeNotSupported()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => instance.Serialize(new Identity()));
        }

        private PrimitiveSerializer CreatePrimitiveSerializer()
        {
            return new PrimitiveSerializer();
        }
    }
}
