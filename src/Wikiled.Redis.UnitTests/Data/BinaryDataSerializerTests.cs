using NUnit.Framework;
using Wikiled.Redis.Data;
using Wikiled.Redis.UnitTests.MockData;

namespace Wikiled.Redis.UnitTests.Data
{
    [TestFixture]
    public class BinaryDataSerializerTests
    {
        [Test]
        public void Serialize()
        {
            var order = new MainDataOne();
            order.Name = "Test";
            var serializerTests = new BinaryDataSerializer();
            var data = serializerTests.Serialize(order);
            var orderResult = serializerTests.Deserialize<MainDataOne>(data);
            Assert.AreNotSame(order, orderResult);
            Assert.AreEqual("Test", orderResult.Name);
        }

        [Test]
        public void SerializeObject()
        {
            var order = new MainDataOne();
            order.Name = "Test";
            var serializerTests = new BinaryDataSerializer();
            var data = serializerTests.Serialize(order);
            var orderResult = (IMainData)serializerTests.Deserialize(typeof(MainDataOne), data);
            Assert.AreNotSame(order, orderResult);
            Assert.AreEqual("Test", orderResult.Name);
        }
    }
}
