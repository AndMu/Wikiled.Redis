using System;
using System.Collections.Generic;
using Wikiled.Redis.Information;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Wikiled.Redis.UnitTests.Information
{
    [TestFixture]
    public class PersistenceInfoTests
    {
        private Dictionary<string, string> allValues;

        [SetUp]
        public void Setup()
        {
            allValues = new Dictionary<string, string>();
            allValues["aof_current_size"] = "100";
            allValues["rdb_bgsave_in_progress"] = "1";
            allValues["aof_rewrite_in_progress"] = "1";
        }

        [Test]
        public void Construct()
        {
            var server = InfoTestsHelper.Create("Persistence", allValues);
            ClassicAssert.Throws<ArgumentNullException>(() => new PersistenceInfo(null));
            PersistenceInfo info = new PersistenceInfo(server);
            ClassicAssert.AreEqual(100, info.AofSize);
            ClassicAssert.AreEqual(1, info.IsAofRewriting);
            ClassicAssert.AreEqual(1, info.IsRdbSaving);
        }

        [Test]
        public void ConstructMissing()
        {
            allValues.Clear();
            var server = InfoTestsHelper.Create("Persistence", allValues);
            PersistenceInfo info = new PersistenceInfo(server);
            ClassicAssert.IsNull(info.AofSize);
            ClassicAssert.IsNull(info.IsAofRewriting);
            ClassicAssert.IsNull(info.IsRdbSaving);
        }
    }
}
