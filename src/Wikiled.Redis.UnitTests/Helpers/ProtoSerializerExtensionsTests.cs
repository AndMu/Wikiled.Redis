using NUnit.Framework;
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
            Assert.AreEqual("Test", result.Name);
            Assert.AreEqual(5, result.Total);
        }

        [Test]
        public void SerializeCompress()
        {
            var data = instance.ProtoSerializeCompress();
            var result = data.ProtoDecompressDeserialize<TestData>();
            Assert.AreEqual("Test", result.Name);
            Assert.AreEqual(5, result.Total);

            result = (TestData)data.ProtoDecompressDeserialize(typeof(TestData));
            Assert.AreEqual("Test", result.Name);
            Assert.AreEqual(5, result.Total);
        }

        [Test]
        public void SmartSerializeCompress()
        {
            var data = instance.SmartSerializeCompress(out bool compressed);
            Assert.IsFalse(compressed);
            Assert.AreEqual(8, data.Length);

            data = instance.SmartSerializeCompress(out compressed, 1);
            Assert.IsTrue(compressed);
            Assert.AreEqual(10, data.Length);
        }

        [Test]
        public void DeserializeByType()
        {
            var data = instance.ProtoSerialize();
            var result = (TestData)data.ProtoDeserialize(typeof(TestData));
            Assert.AreEqual("Test", result.Name);
            Assert.AreEqual(5, result.Total);
        }
    }
}
