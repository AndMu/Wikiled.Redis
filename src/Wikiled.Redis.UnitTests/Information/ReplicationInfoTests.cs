using System;
using System.Collections.Generic;
using Wikiled.Redis.Information;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Wikiled.Redis.UnitTests.Information
{
    [TestFixture]
    public class ReplicationInfoTests
    {
        private Dictionary<string, string> allValues;

        [SetUp]
        public void Setup()
        {
            allValues = new Dictionary<string, string>();
            allValues["role"] = "slave";
            allValues["master_link_status"] = "up";
            allValues["master_last_io_seconds_ago"] = "10";
            allValues["slave_repl_offset"] = "11";
            allValues["master_sync_in_progress"] = "1";
        }

        [Test]
        public void Construct()
        {
            var server = InfoTestsHelper.Create("Replication", allValues);
            ClassicAssert.Throws<ArgumentNullException>(() => new ReplicationInfo(null));
            ReplicationInfo info = new ReplicationInfo(server);
            ClassicAssert.AreEqual(ReplicationRole.Slave, info.Role);
            ClassicAssert.AreEqual(MasterLinkStatus.Up, info.MasterLinkStatus);
            ClassicAssert.AreEqual(10, info.LastSync);
            ClassicAssert.AreEqual(11, info.SlaveReplOffset);
            ClassicAssert.AreEqual(1, info.IsMasterSyncInProgress);
            ClassicAssert.AreEqual(server, info.Main);
        }

        [Test]
        public void ConstructMissing()
        {
            allValues.Clear();
            var server = InfoTestsHelper.Create("Replication", allValues);
            ReplicationInfo info = new ReplicationInfo(server);
            ClassicAssert.IsNull(info.Role);
            ClassicAssert.IsNull(info.LastSync);
            ClassicAssert.IsNull(info.MasterLinkStatus);
            ClassicAssert.IsNull(info.IsMasterSyncInProgress);
            ClassicAssert.IsNull(info.SlaveReplOffset);
        }
    }
}
