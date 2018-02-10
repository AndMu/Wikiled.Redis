using System;
using Moq;
using NUnit.Framework;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Data;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Serialization;

namespace Wikiled.Redis.UnitTests.Logic
{
    [TestFixture]
    public class HandlingDefinitionTests
    {
        private Mock<IDataSerializer> dataSerializer;

        private Mock<IRedisLink> link;

        [SetUp]
        public void Setup()
        {
            link = new Mock<IRedisLink>();
            dataSerializer = new Mock<IDataSerializer>();
        }

        [Test]
        public void ConstructGeneric()
        {
            Assert.Throws<ArgumentException>(() => HandlingDefinition<Identity>.ConstructGeneric(link.Object));
            link.Setup(item => item.State).Returns(ChannelState.Open);
            link.Setup(item => item.LinkId).Returns(-1);
            Assert.Throws<ArgumentException>(() => HandlingDefinition<Identity>.ConstructGeneric(link.Object));

            link.Setup(item => item.LinkId).Returns(10);

            Assert.Throws<ArgumentOutOfRangeException>(() => HandlingDefinition<int>.ConstructGeneric(link.Object, dataSerializer.Object));
            var instance = HandlingDefinition<Identity>.ConstructGeneric(link.Object);
            Assert.IsFalse(instance.IsWellKnown);
            Assert.IsNull(instance.Serializer);
            Assert.AreEqual("L10:1", instance.GetNextId());
        }

        [Test]
        public void ConstructRefType()
        {
            link.Setup(item => item.State).Returns(ChannelState.Open);
            link.Setup(item => item.LinkId).Returns(-1);
            link.Setup(item => item.LinkId).Returns(10);
            var definition = HandlingDefinition<Identity>.ConstructGeneric(link.Object);
            Assert.IsFalse(definition.IsNormalized);
            Assert.IsFalse(definition.IsSingleInstance);
            Assert.IsFalse(definition.IsWellKnown);
            Assert.IsNull(definition.Serializer);
            definition.IsNormalized = true;
            definition.IsSingleInstance = true;
            definition.IsWellKnown = true;

            Mock<IKeyValueSerializer<Identity>> serializer = new Mock<IKeyValueSerializer<Identity>>();
            definition.Serializer = serializer.Object;
            Assert.IsTrue(definition.IsNormalized);
            Assert.IsTrue(definition.IsSingleInstance);
            Assert.IsTrue(definition.IsWellKnown);
            Assert.IsNotNull(definition.Serializer);
        }

        [Test]
        public void TestPrimitiveType()
        {
            link.Setup(item => item.State).Returns(ChannelState.Open);
            link.Setup(item => item.LinkId).Returns(-1);
            link.Setup(item => item.LinkId).Returns(10);
            var definition = HandlingDefinition<int>.ConstructGeneric(link.Object);
            Assert.Throws<ArgumentOutOfRangeException>(() => definition.IsNormalized = true);
            Assert.Throws<ArgumentOutOfRangeException>(() => definition.IsSingleInstance = true);
            Assert.Throws<ArgumentOutOfRangeException>(() => definition.IsWellKnown = true);
            Mock<IKeyValueSerializer<int>> serializer = new Mock<IKeyValueSerializer<int>>();
            Assert.Throws<ArgumentOutOfRangeException>(() => definition.Serializer = serializer.Object);
            Assert.IsFalse(definition.IsNormalized);
            Assert.IsFalse(definition.IsSingleInstance);
            Assert.IsFalse(definition.IsWellKnown);
            Assert.IsNull(definition.Serializer);
        }
    }
}
