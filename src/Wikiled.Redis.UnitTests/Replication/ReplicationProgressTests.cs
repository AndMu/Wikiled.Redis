using System;
using NUnit.Framework;
using System.Net;
using Wikiled.Redis.Replication;

namespace Wikiled.Redis.UnitTests.Replication
{
    [TestFixture]
    public class ReplicationProgressTests
    {
        private HostStatus master;

        private HostStatus[] slaves;

        [SetUp]
        public void Setup()
        {
            master = new HostStatus(new DnsEndPoint("Test", 1000), 2000);
            slaves = new[]
                     {
                         new HostStatus(new DnsEndPoint("Test", 1000), 1000)
                     };
        }

        [Test]
        public void CheckInActive()
        {
            var progress = ReplicationProgress.CreateInActive();
            Assert.IsFalse(progress.IsActive);
            Assert.IsFalse(progress.InSync);
        }

        [Test]
        public void CheckActive()
        {
            var progress = ReplicationProgress.CreateActive(master, slaves);
            Assert.IsTrue(progress.IsActive);
            Assert.IsFalse(progress.InSync);
            slaves[0] = master;
            progress = ReplicationProgress.CreateActive(master, slaves);
            Assert.IsTrue(progress.InSync);
        }

        [Test]
        public void CheckActiveArguments()
        {
            Assert.Throws<ArgumentNullException>(() => ReplicationProgress.CreateActive(null, slaves));
            Assert.Throws<ArgumentNullException>(() => ReplicationProgress.CreateActive(master, null));
            Assert.Throws<ArgumentException>(() => ReplicationProgress.CreateActive(master));
        }
    }
}
