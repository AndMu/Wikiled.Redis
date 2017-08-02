using System;
using System.Net;
using NUnit.Framework;
using Wikiled.Redis.Information;

namespace Wikiled.Redis.UnitTests.Information
{
    [TestFixture]
    public class SlaveInformationTests
    {
        [Test]
        public void Parse()
        {
            var information = SlaveInformation.Parse("ip=127.0.0.1,port=6027,state=online,offset=239,lag=0");
            Assert.AreEqual(IPAddress.Parse("127.0.0.1"), information.EndPoint.Address);
            Assert.AreEqual(6027, information.EndPoint.Port);
            Assert.AreEqual("online", information.State);
            Assert.AreEqual(239, information.Offset);
            Assert.AreEqual(0, information.Lag);
        }


        [Test]
        public void ParseInvalid()
        {
            Assert.Throws<ArgumentNullException>(() => SlaveInformation.Parse(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => SlaveInformation.Parse("ip=127.0.0.1,port=6027,state=online"));
        }
    }
}
