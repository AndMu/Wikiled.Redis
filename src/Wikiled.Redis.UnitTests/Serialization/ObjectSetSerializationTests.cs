using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using System;
using System.Linq;
using NUnit.Framework.Legacy;
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
            instance = new ObjectHashSetSerialization<MainDataOne>(new NullLogger<ObjectHashSetSerialization<MainDataOne>>(), new XmlDataSerializer(), false);
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
            ClassicAssert.AreEqual(3, columns.Length);
        }

        [Test]
        public void GetEntries()
        {
            ClassicAssert.Throws<ArgumentNullException>(() => instance.GetEntries(null).ToArray());
            var entries = instance.GetEntries(new MainDataOne()).ToArray();
            ClassicAssert.AreEqual(3, entries.Length);
        }

        [Test]
        public void GetInstances()
        {
            ClassicAssert.Throws<ArgumentNullException>(() => instance.GetInstances(null).ToArray());
            var data = instance.GetInstances(instance.GetEntries(new MainDataOne()).Select(item => item.Value).ToArray());
            ClassicAssert.IsNotNull(data);
        }
    }
}
