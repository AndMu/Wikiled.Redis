using System;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Wikiled.Common.Extensions;
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
            serializer = new KeyValueSerializer<TestType>(new NullLogger<KeyValueSerializer<TestType>>());
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
            ClassicAssert.AreEqual(5, serializer.Properties.Length);
        }

        [Test]
        public void Serialize()
        {
            var keys = serializer.Serialize(data).ToArray();
            ClassicAssert.AreEqual(5, keys.Length);
            var result = serializer.Deserialize(keys);
            ClassicAssert.AreEqual(data.Status1, result.Status1);
            ClassicAssert.AreEqual(data.Data, result.Data);
            ClassicAssert.AreEqual(data.Value, result.Value);
            ClassicAssert.AreEqual(data.Another, result.Another);
            ClassicAssert.AreEqual(data.Date, result.Date);
        }
    }
}
