using NUnit.Framework;
using Wikiled.Redis.Data;
using Wikiled.Redis.UnitTests.MockData;

namespace Wikiled.Redis.UnitTests.Data
{
    [TestFixture]
    public class XmlDataSerializerTests
    {
        [TestCase(true, 66)]
        [TestCase(false, 100)]
        public void Serialize(bool serialize, int size)
        {
            var order = new MainDataOne();
            order.Name = "Test";
            XmlDataSerializer serializerTests = new XmlDataSerializer(serialize);
            var data = serializerTests.Serialize(order);
            var orderResult = serializerTests.Deserialize<MainDataOne>(data);
            Assert.AreNotSame(order, orderResult);
            Assert.AreEqual(size, data.Length);
            Assert.AreEqual("Test", orderResult.Name);
        }

        [TestCase(true, 66)]
        [TestCase(false, 100)]
        public void SerializeObject(bool serialize, int size)
        {
            var order = new MainDataOne();
            order.Name = "Test";
            var serializerTests = new XmlDataSerializer(serialize);
            var data = serializerTests.Serialize(order);
            var orderResult = (IMainData)serializerTests.Deserialize(typeof(MainDataOne), data);
            Assert.AreNotSame(order, orderResult);
            Assert.AreEqual(size, data.Length);
            Assert.AreEqual("Test", orderResult.Name);
        }
    }
}
