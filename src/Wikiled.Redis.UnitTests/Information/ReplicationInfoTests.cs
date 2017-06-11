using System;
using System.Collections.Generic;
using Wikiled.Redis.Information;
using NUnit.Framework;

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
            Assert.Throws<ArgumentNullException>(() => new ReplicationInfo(null));
            ReplicationInfo info = new ReplicationInfo(server);
            Assert.AreEqual(ReplicationRole.Slave, info.Role);
            Assert.AreEqual(MasterLinkStatus.Up, info.MasterLinkStatus);
            Assert.AreEqual(10, info.LastSync);
            Assert.AreEqual(11, info.SlaveReplOffset);
            Assert.AreEqual(1, info.IsMasterSyncInProgress);
            Assert.AreEqual(server, info.Main);
        }

        [Test]
        public void ConstructMissing()
        {
            allValues.Clear();
            var server = InfoTestsHelper.Create("Replication", allValues);
            ReplicationInfo info = new ReplicationInfo(server);
            Assert.IsNull(info.Role);
            Assert.IsNull(info.LastSync);
            Assert.IsNull(info.MasterLinkStatus);
            Assert.IsNull(info.IsMasterSyncInProgress);
            Assert.IsNull(info.SlaveReplOffset);
        }
    }
}
