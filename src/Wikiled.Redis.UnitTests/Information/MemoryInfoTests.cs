using System;
using System.Collections.Generic;
using Wikiled.Redis.Information;
using NUnit.Framework;

namespace Wikiled.Redis.UnitTests.Information
{
    [TestFixture]
    public class MemoryInfoTests
    {
        private Dictionary<string, string> allValues;

        [SetUp]
        public void Setup()
        {
            allValues = new Dictionary<string, string>();
            allValues["used_memory"] = "100";
            allValues["mem_fragmentation_ratio"] = "0.7";
        }

        [Test]
        public void Construct()
        {
            var server = InfoTestsHelper.Create("Memory", allValues);
            Assert.Throws<ArgumentNullException>(() => new MemoryInfo(null));
            MemoryInfo info = new MemoryInfo(server);
            Assert.AreEqual(100, info.UsedMemory);
            Assert.AreEqual(0.7, info.MemoryFragmentation);
            Assert.AreEqual(server, info.Main);
        }

        [Test]
        public void ConstructMissing()
        {
            allValues.Clear();
            var server = InfoTestsHelper.Create("Memory", allValues);
            MemoryInfo info = new MemoryInfo(server);
            Assert.IsNull(info.UsedMemory);
            Assert.IsNull(info.MemoryFragmentation);
        }
    }
}
