using System;
using System.Linq;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Serialization;
using Moq;
using NUnit.Framework;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Data;
using Wikiled.Redis.UnitTests.MockData;

namespace Wikiled.Redis.UnitTests.Serialization
{
    [TestFixture]
    public class ObjectSetSerializationTests
    {
        private ObjectHashSetSerialization instance;

        private Mock<IRedisLink> link;

        [SetUp]
        public void Setup()
        {
            link = new Mock<IRedisLink>();
            link.Setup(item => item.State).Returns(ChannelState.Open);
            link.Setup(item => item.LinkId).Returns(0);
            link.Setup(item => item.GetDefinition<MainDataOne>()).Returns(HandlingDefinition<MainDataOne>.ConstructGeneric(link.Object));
            instance = new ObjectHashSetSerialization(link.Object, new FlatProtoDataSerializer(true));
        }

        [Test]
        public void Construct()
        {
            Assert.Throws<ArgumentNullException>(() => new ObjectHashSetSerialization(null, new FlatProtoDataSerializer(true)));
            Assert.Throws<ArgumentNullException>(() => new ObjectHashSetSerialization(link.Object, null));
        }

        [Test]
        public void GetColumns()
        {
            var columns = instance.GetColumns<MainDataOne>();
            Assert.AreEqual(3, columns.Length);
        }

        [Test]
        public void GetEntries()
        {
            Assert.Throws<ArgumentNullException>(() => instance.GetEntries<MainDataOne>(null).ToArray());
            var entries = instance.GetEntries(new MainDataOne()).ToArray();
            Assert.AreEqual(3, entries.Length);
        }

        [Test]
        public void GetInstances()
        {
            Assert.Throws<ArgumentNullException>(() => instance.GetInstances<MainDataOne>(null).ToArray());
            var data = instance.GetInstances<MainDataOne>(instance.GetEntries(new MainDataOne()).Select(item => item.Value).ToArray());
            Assert.IsNotNull(data);
        }
    }
}
