using Microsoft.IO;
using Wikiled.Redis.Data;
using NUnit.Framework;
using Wikiled.Redis.UnitTests.MockData;

namespace Wikiled.Redis.UnitTests.Data
{
    [TestFixture]
    public class FlatProtoDataSerializerTests
    {
        private RecyclableMemoryStreamManager stream = new RecyclableMemoryStreamManager();

        [Test]
        public void SerializeByInterface()
        {
            var order = new MainDataOne();
            order.Name = "Test";
            FlatProtoDataSerializer serializerTests = new FlatProtoDataSerializer(false, stream);
            var data = serializerTests.Serialize<IMainData>(order);
            var orderResult = serializerTests.Deserialize<IMainData>(data);
            Assert.AreNotSame(order, orderResult);
            Assert.AreEqual(116, data.Length);
            Assert.AreEqual("Test", orderResult.Name);
        }

        [Test]
        public void SerializeObject()
        {
            var order = new MainDataOne();
            order.Name = "Test";
            var serializerTests = new FlatProtoDataSerializer(false, stream);
            var data = serializerTests.Serialize<IMainData>(order);
            var orderResult = (IMainData)serializerTests.Deserialize(typeof(IMainData), data);
            Assert.AreNotSame(order, orderResult);
            Assert.AreEqual(116, data.Length);
            Assert.AreEqual("Test", orderResult.Name);
        }

        [Test]
        public void SerializeComplex()
        {
            var order = new MainDataComplex();
            order.One = new MainDataOne();
            order.One.Name = "Test1";
            order.Two = new MainDataTwo();
            order.Two.Name = "Test2";

            var serializerTests = new FlatProtoDataSerializer(false, stream);
            var data = serializerTests.Serialize(order);
            var orderResult = (MainDataComplex)serializerTests.Deserialize(typeof(MainDataComplex), data);
            Assert.AreNotSame(order, orderResult);
            Assert.AreEqual(116, data.Length);
            Assert.AreEqual("Test1", orderResult.One.Name);
        }


        [Test]
        public void SerializeWellknown()
        {
            var order = new MainDataOne();
            order.Name = "Test";
            FlatProtoDataSerializer serializer = new FlatProtoDataSerializer(true, stream);
            var data = serializer.Serialize(order);
            var orderResult = serializer.Deserialize<MainDataOne>(data);
            Assert.AreNotSame(order, orderResult);
            Assert.AreEqual(36, data.Length);
            Assert.AreEqual("Test", orderResult.Name);
        }
    }
}
