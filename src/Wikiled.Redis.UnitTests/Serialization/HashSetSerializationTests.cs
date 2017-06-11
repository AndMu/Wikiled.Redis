using System;
using System.Linq;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Serialization;
using Moq;
using NUnit.Framework;
using Wikiled.Redis.Channels;

namespace Wikiled.Redis.UnitTests.Serialization
{
    [TestFixture]
    public class HashSetSerializationTests
    {
        private HashSetSerialization instance;

        private Mock<IRedisLink> link;

        [SetUp]
        public void Setup()
        {
            link = new Mock<IRedisLink>();
            link.Setup(item => item.State).Returns(ChannelState.Open);
            link.Setup(item => item.LinkId).Returns(0);
            link.Setup(item => item.GetDefinition<Identity>())
                .Returns(
                HandlingDefinition<Identity>.ConstructKeyValue(link.Object, new KeyValueSerializer<Identity>(() => new Identity())));
            instance = new HashSetSerialization(link.Object);
        }

        [Test]
        public void Construct()
        {
            Assert.Throws<ArgumentNullException>(() => new HashSetSerialization(null));
        }

        [Test]
        public void GetColumns()
        {
            var columns = instance.GetColumns<Identity>();
            Assert.AreEqual(4, columns.Length);
        }

        [Test]
        public void GetEntries()
        {
            Assert.Throws<ArgumentNullException>(() => instance.GetEntries<Identity>(null).ToArray());
            var entries = instance.GetEntries(new Identity()).ToArray();
            Assert.AreEqual(4, entries.Length);
        }

        [Test]
        public void GetInstances()
        {
            Assert.Throws<ArgumentNullException>(() => instance.GetInstances<Identity>(null).ToArray());
            var data = instance.GetInstances<Identity>(instance.GetEntries(new Identity()).Select(item => item.Value).ToArray());
            Assert.IsNotNull(data);
        }
    }
}
