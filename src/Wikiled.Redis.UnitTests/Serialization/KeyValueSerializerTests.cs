using System;
using System.Linq;
using NUnit.Framework;
using Wikiled.Core.Utility.Extensions;
using Wikiled.Redis.Serialization;

namespace Wikiled.Redis.UnitTests.Serialization
{
    [TestFixture]
    public class KeyValueSerializerTests
    {
        private KeyValueSerializer<TestType> serializer;

        private TestType data;

        [SetUp]
        public void Setup()
        {
            serializer = new KeyValueSerializer<TestType>(() => new TestType());
            data = new TestType();
            data.Status1 = BasicTypes.Char;
            data.Data = "TestId";
            data.Value = 1;
            data.Another = 2;
            data.Date = new DateTime(2012, 02, 23);
        }

        [Test]
        public void Construct()
        {
            Assert.AreEqual(5, serializer.Properties.Length);
        }

        [Test]
        public void Serialize()
        {
            var keys = serializer.Serialize(data).ToArray();
            Assert.AreEqual(5, keys.Length);
            var result = serializer.Deserialize(keys);
            Assert.AreEqual(data.Status1, result.Status1);
            Assert.AreEqual(data.Data, result.Data);
            Assert.AreEqual(data.Value, result.Value);
            Assert.AreEqual(data.Another, result.Another);
            Assert.AreEqual(data.Date, result.Date);
        }
    }
}
