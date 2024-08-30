using System;
using System.Net;
using NUnit.Framework;
using NUnit.Framework.Legacy;
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
            ClassicAssert.AreEqual(IPAddress.Parse("127.0.0.1"), information.EndPoint.Address);
            ClassicAssert.AreEqual(6027, information.EndPoint.Port);
            ClassicAssert.AreEqual("online", information.State);
            ClassicAssert.AreEqual(239, information.Offset);
            ClassicAssert.AreEqual(0, information.Lag);
        }


        [Test]
        public void ParseInvalid()
        {
            ClassicAssert.Throws<ArgumentException>(() => SlaveInformation.Parse(null));
            ClassicAssert.Throws<ArgumentOutOfRangeException>(() => SlaveInformation.Parse("ip=127.0.0.1,port=6027,state=online"));
        }
    }
}
