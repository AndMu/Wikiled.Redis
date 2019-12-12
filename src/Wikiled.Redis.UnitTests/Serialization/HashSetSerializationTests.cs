using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using Wikiled.Common.Testing.Utilities.Reflection;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Serialization;

namespace Wikiled.Redis.UnitTests.Serialization
{
    [TestFixture]
    public class HashSetSerializationTests
    {
        private HashSetSerialization<Identity> instance;

        private Mock<IRedisLink> link;

        [SetUp]
        public void Setup()
        {
            link = new Mock<IRedisLink>();
            link.Setup(item => item.State).Returns(ChannelState.Open);
            link.Setup(item => item.LinkId).Returns(0);
            instance = new HashSetSerialization<Identity>(new NullLogger<HashSetSerialization<Identity>>(), new KeyValueSerializer<Identity>());
        }

        [Test]
        public void Construct()
        {
            ConstructorHelper.ConstructorMustThrowArgumentNullException<HashSetSerialization<Identity>>();
        }

        [Test]
        public void GetColumns()
        {
            var columns = instance.GetColumns();
            Assert.AreEqual(4, columns.Length);
        }

        [Test]
        public void GetEntries()
        {
            Assert.Throws<ArgumentNullException>(() => instance.GetEntries(null).ToArray());
            var entries = instance.GetEntries(new Identity()).ToArray();
            Assert.AreEqual(4, entries.Length);
        }

        [Test]
        public void GetInstances()
        {
            Assert.Throws<ArgumentNullException>(() => instance.GetInstances(null).ToArray());
            var data = instance.GetInstances(instance.GetEntries(new Identity()).Select(item => item.Value).ToArray());
            Assert.IsNotNull(data);
        }
    }
}
