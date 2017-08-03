using NUnit.Framework;
using System.Linq;
using System.Net;
using Wikiled.Redis.Replication;

namespace Wikiled.Redis.UnitTests.Replication
{
    [TestFixture]
    public class ReplicationProgressExtensionTests
    {
        private HostStatus master;

        private HostStatus[] slaves;

        private ReplicationProgress instance;

        [SetUp]
        public void Setup()
        {
            master = new HostStatus(new DnsEndPoint("Test", 1000), 2000);
            slaves = new[]
                     {
                         new HostStatus(new DnsEndPoint("Test", 1000), 1000)
                     };

            instance = ReplicationProgress.CreateActive(
                master,
                slaves);
        }

        [Test]
        public void Construct()
        {
            var result = instance.GenerateProgress().ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(2, result[0]);
        }
    }
}
