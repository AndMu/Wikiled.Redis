using Microsoft.IO;
using Wikiled.Redis.Data;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Wikiled.Redis.UnitTests.MockData;

namespace Wikiled.Redis.UnitTests.Data
{
    [TestFixture]
    public class FlatProtoDataSerializerTests
    {
        private readonly RecyclableMemoryStreamManager stream = new RecyclableMemoryStreamManager();

        [Test]
        public void SerializeByInterface()
        {
            var order = new MainDataOne();
            order.Name = "Test";
            FlatProtoDataSerializer serializerTests = new FlatProtoDataSerializer(false, stream);
            var data = serializerTests.Serialize<IMainData>(order);
            var orderResult = serializerTests.Deserialize<IMainData>(data);
            ClassicAssert.AreNotSame(order, orderResult);
            ClassicAssert.AreEqual(116, data.Length);
            ClassicAssert.AreEqual("Test", orderResult.Name);
        }

        [Test]
        public void SerializeObject()
        {
            var order = new MainDataOne();
            order.Name = "Test";
            var serializerTests = new FlatProtoDataSerializer(false, stream);
            var data = serializerTests.Serialize<IMainData>(order);
            var orderResult = (IMainData)serializerTests.Deserialize(typeof(IMainData), data);
            ClassicAssert.AreNotSame(order, orderResult);
            ClassicAssert.AreEqual(116, data.Length);
            ClassicAssert.AreEqual("Test", orderResult.Name);
        }

        [Test]
        public void SerializeWellknown()
        {
            var order = new MainDataOne();
            order.Name = "Test";
            FlatProtoDataSerializer serializer = new FlatProtoDataSerializer(true, stream);
            var data = serializer.Serialize(order);
            var orderResult = serializer.Deserialize<MainDataOne>(data);
            ClassicAssert.AreNotSame(order, orderResult);
            ClassicAssert.AreEqual(36, data.Length);
            ClassicAssert.AreEqual("Test", orderResult.Name);
        }
    }
}
