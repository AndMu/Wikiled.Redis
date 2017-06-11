using System;
using Wikiled.Redis.Logic;
using Moq;
using NUnit.Framework;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Data;
using Wikiled.Redis.Serialization;

namespace Wikiled.Redis.UnitTests.Logic
{
    [TestFixture]
    public class HandlingDefinitionTests
    {
        private Mock<IRedisLink> link;

        private Mock<IDataSerializer> dataSerializer;

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
        public void ConstructWellKnown()
        {
            Assert.Throws<ArgumentException>(() => HandlingDefinition<Identity>.ConstructWellKnown(link.Object));
            link.Setup(item => item.State).Returns(ChannelState.Open);
            link.Setup(item => item.LinkId).Returns(-1);
            Assert.Throws<ArgumentException>(() => HandlingDefinition<Identity>.ConstructWellKnown(link.Object));

            link.Setup(item => item.LinkId).Returns(10);
            Assert.Throws<ArgumentOutOfRangeException>(() => HandlingDefinition<int>.ConstructWellKnown(link.Object));
            var instance = HandlingDefinition<Identity>.ConstructWellKnown(link.Object);
            Assert.IsTrue(instance.IsWellKnown);
            Assert.IsNull(instance.Serializer);
            Assert.AreEqual("L10:1", instance.GetNextId());
            Assert.AreEqual("L10:2", instance.GetNextId());
        }

        [Test]
        public void TestPrimitiveType()
        {
            link.Setup(item => item.State).Returns(ChannelState.Open);
            link.Setup(item => item.LinkId).Returns(-1);
            link.Setup(item => item.LinkId).Returns(10);
            var definition = HandlingDefinition<int>.ConstructGeneric(link.Object);
            Assert.Throws<ArgumentOutOfRangeException>(() => definition.IsSingleInstance = true);
            Assert.Throws<ArgumentOutOfRangeException>(() => definition.ExtractType = true);
        }

        [Test]
        public void ConstructKeyValue()
        {
            Assert.Throws<ArgumentException>(
                () =>
                HandlingDefinition<Identity>.ConstructKeyValue(
                    link.Object,
                    new KeyValueSerializer<Identity>(() => new Identity())));

            link.Setup(item => item.State).Returns(ChannelState.Open);
            link.Setup(item => item.LinkId).Returns(-1);
            Assert.Throws<ArgumentException>(
                () =>
                HandlingDefinition<Identity>.ConstructKeyValue(
                    link.Object,
                    new KeyValueSerializer<Identity>(() => new Identity())));

            link.Setup(item => item.LinkId).Returns(1);

            var instance = HandlingDefinition<Identity>.ConstructKeyValue(
                link.Object,
                new KeyValueSerializer<Identity>(() => new Identity()));
            Assert.IsTrue(instance.IsWellKnown);
            Assert.IsNotNull(instance.Serializer);
            Assert.AreEqual("L1:1", instance.GetNextId());
        }
    }
}
