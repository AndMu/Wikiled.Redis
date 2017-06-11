using System;
using System.Linq;
using Wikiled.Common.Channels;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Serialization;
using Wikiled.Redis.UnitTests.TestData;
using Moq;
using NUnit.Framework;

namespace Wikiled.Redis.UnitTests.Serialization
{
    [TestFixture]
    public class ProtoSetSerializationTests
    {
        private ProtoSetSerialization instance;

        private Mock<IRedisLink> link;

        [SetUp]
        public void Setup()
        {
            link = new Mock<IRedisLink>();
            link.Setup(item => item.State).Returns(ChannelState.Open);
            link.Setup(item => item.LinkId).Returns(0);
            link.Setup(item => item.GetDefinition<MainDataOne>()).Returns(HandlingDefinition<MainDataOne>.ConstructGeneric(link.Object));
            instance = new ProtoSetSerialization(link.Object);
        }

        [Test]
        public void Construct()
        {
            Assert.Throws<ArgumentNullException>(() => new ProtoSetSerialization(null));
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
            Assert.Throws<ArgumentNullException>(() => instance.GetEntries<MainDataOne>(null));
            var entries = instance.GetEntries(new MainDataOne()).ToArray();
            Assert.AreEqual(4, entries.Length);
        }

        [Test]
        public void GetInstances()
        {
            Assert.Throws<ArgumentNullException>(() => instance.GetInstances<MainDataOne>(null));
            var data = instance.GetInstances<MainDataOne>(instance.GetEntries(new MainDataOne()).Select(item => item.Value).ToArray());
            Assert.IsNotNull(data);
        }
    }
}
