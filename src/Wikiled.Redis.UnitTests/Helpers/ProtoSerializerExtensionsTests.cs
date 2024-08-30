using NUnit.Framework;
using NUnit.Framework.Legacy;
using Wikiled.Redis.Helpers;

namespace Wikiled.Redis.UnitTests.Helpers
{
    [TestFixture]
    public class ProtoSerializerExtensionsTests
    {
        private TestData instance;

        [SetUp]
        public void Setup()
        {
            instance = new TestData();
            instance.Name = "Test";
            instance.Total = 5;
        }

        [Test]
        public void Serialize()
        {
            var data = instance.ProtoSerialize();
            var result = data.ProtoDeserialize<TestData>();
            ClassicAssert.AreEqual("Test", result.Name);
            ClassicAssert.AreEqual(5, result.Total);
        }

        [Test]
        public void SerializeCompress()
        {
            var data = instance.ProtoSerializeCompress();
            var result = data.ProtoDecompressDeserialize<TestData>();
            ClassicAssert.AreEqual("Test", result.Name);
            ClassicAssert.AreEqual(5, result.Total);

            result = (TestData)data.ProtoDecompressDeserialize(typeof(TestData));
            ClassicAssert.AreEqual("Test", result.Name);
            ClassicAssert.AreEqual(5, result.Total);
        }

        [Test]
        public void SmartSerializeCompress()
        {
            var data = instance.SmartSerializeCompress(out bool compressed);
            ClassicAssert.IsFalse(compressed);
            ClassicAssert.AreEqual(8, data.Length);

            data = instance.SmartSerializeCompress(out compressed, 1);
            ClassicAssert.IsTrue(compressed);
            ClassicAssert.AreEqual(10, data.Length);
        }

        [Test]
        public void DeserializeByType()
        {
            var data = instance.ProtoSerialize();
            var result = (TestData)data.ProtoDeserialize(typeof(TestData));
            ClassicAssert.AreEqual("Test", result.Name);
            ClassicAssert.AreEqual(5, result.Total);
        }
    }
}
