using NUnit.Framework;
using NUnit.Framework.Legacy;
using Wikiled.Redis.Data;
using Wikiled.Redis.UnitTests.MockData;

namespace Wikiled.Redis.UnitTests.Data
{
    [TestFixture]
    public class ZipXmlDataSerializerTests
    {
        [Test]
        public void Serialize()
        {
            var order = new MainDataOne();
            order.Name = "Test";
            var serializerTests = new ZipXmlDataSerializer();
            var data = serializerTests.Serialize(order);
            var orderResult = serializerTests.Deserialize<MainDataOne>(data);
            ClassicAssert.AreNotSame(order, orderResult);
            ClassicAssert.AreEqual(79, data.Length);
            ClassicAssert.AreEqual("Test", orderResult.Name);
        }

        [Test]
        public void SerializeObject()
        {
            var order = new MainDataOne();
            order.Name = "Test";
            var serializerTests = new ZipXmlDataSerializer();
            var data = serializerTests.Serialize(order);
            var orderResult = (IMainData)serializerTests.Deserialize(typeof(MainDataOne), data);
            ClassicAssert.AreNotSame(order, orderResult);
            ClassicAssert.AreEqual(79, data.Length);
            ClassicAssert.AreEqual("Test", orderResult.Name);
        }
    }
}
