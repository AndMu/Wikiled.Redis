using System;
using System.Linq;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Serialization;
using Moq;
using NUnit.Framework;
using Wikiled.Common.Testing.Utilities.Reflection;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Data;
using Wikiled.Redis.UnitTests.MockData;

namespace Wikiled.Redis.UnitTests.Serialization
{
    [TestFixture]
    public class ObjectSetSerializationTests
    {
        private ObjectHashSetSerialization<MainDataOne> instance;

        private Mock<IRedisLink> link;

        [SetUp]
        public void Setup()
        {
            link = new Mock<IRedisLink>();
            link.Setup(item => item.State).Returns(ChannelState.Open);
            link.Setup(item => item.LinkId).Returns(0);
            instance = new ObjectHashSetSerialization<MainDataOne>(link.Object, new BinaryDataSerializer(), false);
        }

        [Test]
        public void Construct()
        {
            ConstructorHelper.ConstructorMustThrowArgumentNullException<ObjectHashSetSerialization<Identity>>();
        }

        [Test]
        public void GetColumns()
        {
            var columns = instance.GetColumns();
            Assert.AreEqual(3, columns.Length);
        }

        [Test]
        public void GetEntries()
        {
            Assert.Throws<ArgumentNullException>(() => instance.GetEntries(null).ToArray());
            var entries = instance.GetEntries(new MainDataOne()).ToArray();
            Assert.AreEqual(3, entries.Length);
        }

        [Test]
        public void GetInstances()
        {
            Assert.Throws<ArgumentNullException>(() => instance.GetInstances(null).ToArray());
            var data = instance.GetInstances(instance.GetEntries(new MainDataOne()).Select(item => item.Value).ToArray());
            Assert.IsNotNull(data);
        }
    }
}
