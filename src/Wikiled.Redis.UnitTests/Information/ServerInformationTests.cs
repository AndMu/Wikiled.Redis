using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Wikiled.Redis.Information;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;

namespace Wikiled.Redis.UnitTests.Information
{
    [TestFixture]
    public class ServerInformationTests
    {
        private IGrouping<string, KeyValuePair<string, string>>[] data;

        private Mock<IServer> server;

        [SetUp]
        public void Setup()
        {
            server = new Mock<IServer>();
            server.Setup(item => item.EndPoint).Returns(new IPEndPoint(IPAddress.Loopback, 6666));
            Dictionary<string, string> table = new Dictionary<string, string>();
            table["Property"] = "value";
            data = table.Select(item => item).GroupBy(item => item.Key).ToArray();
        }

        [Test]
        public void Construct()
        {
            Assert.Throws<ArgumentNullException>(() => new ServerInformation(null, data));
            Assert.Throws<ArgumentNullException>(() => new ServerInformation(server.Object, null));
            ServerInformation information = new ServerInformation(server.Object, data);
            Assert.IsNotNull(information.Memory);
            Assert.IsNotNull(information.Persistence);
            Assert.IsNotNull(information.Stats);
            Assert.IsNotNull(information.Server);
            Assert.AreEqual(1, information.RawData.Count);
        }
    }
}
