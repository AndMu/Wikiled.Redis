using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using System;
using System.Linq;
using Wikiled.Common.Testing.Utilities.Reflection;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Data;
using Wikiled.Redis.Serialization;
using Wikiled.Redis.UnitTests.MockData;

namespace Wikiled.Redis.UnitTests.Serialization
{
    [TestFixture]
    public class ObjectSetSerializationTests
    {
        private ObjectHashSetSerialization<MainDataOne> instance;

        [SetUp]
        public void Setup()
        {
            instance = new ObjectHashSetSerialization<MainDataOne>(new NullLogger<ObjectHashSetSerialization<MainDataOne>>(), new BinaryDataSerializer(), false);
        }

        [Test]
        public void Construct()
        {
            ConstructorHelper.ConstructorMustThrowArgumentNullException<ObjectHashSetSerialization<Identity>>(false);
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
