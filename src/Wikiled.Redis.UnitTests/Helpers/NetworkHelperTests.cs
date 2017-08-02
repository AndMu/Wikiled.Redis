using NUnit.Framework;
using System.Net;
using Wikiled.Redis.Helpers;

namespace Wikiled.Redis.UnitTests.Helpers
{
    [TestFixture]
    public class NetworkHelperTests
    {
        [Test]
        public void GetAddress()
        {
            var address = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2020).GetAddress();
            Assert.AreEqual("127.0.0.1:2020", address);
        }

        [Test]
        public void GetAddressHost()
        {
            var address = new DnsEndPoint("localhost", 2020).GetAddress();
            Assert.AreEqual("127.0.0.1:2020", address);
        }
    }
}
