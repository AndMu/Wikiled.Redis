﻿using Wikiled.Redis.Data;
using NUnit.Framework;
using Wikiled.Redis.UnitTests.MockData;

namespace Wikiled.Redis.UnitTests.Data
{
    [TestFixture]
    public class FlatProtoDataSerializerTests
    {
        [Test]
        public void SerializeByInterface()
        {
            var order = new MainDataOne();
            order.Name = "Test";
            FlatProtoDataSerializer serializerTests = new FlatProtoDataSerializer(false);
            var data = (byte[])serializerTests.Serialize<IMainData>(order);
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
            FlatProtoDataSerializer serializerTests = new FlatProtoDataSerializer(false);
            var data = (byte[])serializerTests.Serialize<IMainData>(order);
            var orderResult = (IMainData)serializerTests.Deserialize(typeof(IMainData), data);
            Assert.AreNotSame(order, orderResult);
            Assert.AreEqual(116, data.Length);
            Assert.AreEqual("Test", orderResult.Name);
        }


        [Test]
        public void SerializeWellknown()
        {
            var order = new MainDataOne();
            order.Name = "Test";
            FlatProtoDataSerializer serializer = new FlatProtoDataSerializer(true);
            var data = (byte[])serializer.Serialize(order);
            var orderResult = serializer.Deserialize<MainDataOne>(data);
            Assert.AreNotSame(order, orderResult);
            Assert.AreEqual(36, data.Length);
            Assert.AreEqual("Test", orderResult.Name);
        }
    }
}
