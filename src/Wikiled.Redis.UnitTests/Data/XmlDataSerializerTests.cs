using NUnit.Framework;
using NUnit.Framework.Legacy;
using Wikiled.Redis.Data;
using Wikiled.Redis.UnitTests.MockData;

namespace Wikiled.Redis.UnitTests.Data
{
    [TestFixture]
    public class XmlDataSerializerTests
    {
        [TestCase(true, 67)]
        [TestCase(false, 100)]
        public void Serialize(bool serialize, int size)
        {
            var order = new MainDataOne();
            order.Name = "Test";
            XmlDataSerializer serializerTests = new XmlDataSerializer(serialize);
            var data = serializerTests.Serialize(order);
            var orderResult = serializerTests.Deserialize<MainDataOne>(data);
            ClassicAssert.AreNotSame(order, orderResult);
            ClassicAssert.GreaterOrEqual(size, data.Length);
            ClassicAssert.AreEqual("Test", orderResult.Name);
        }

        [TestCase(true, 67)]
        [TestCase(false, 100)]
        public void SerializeObject(bool serialize, int size)
        {
            var order = new MainDataOne();
            order.Name = "Test";
            var serializerTests = new XmlDataSerializer(serialize);
            var data = serializerTests.Serialize(order);
            var orderResult = (IMainData)serializerTests.Deserialize(typeof(MainDataOne), data);
            ClassicAssert.AreNotSame(order, orderResult);
            ClassicAssert.GreaterOrEqual(size, data.Length);
            ClassicAssert.AreEqual("Test", orderResult.Name);
        }
    }
}
