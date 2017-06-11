using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Wikiled.Redis.Information;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Replication;
using Moq;
using NUnit.Framework;
using Wikiled.Redis.Channels;

namespace Wikiled.Redis.UnitTests.Replication
{
    [TestFixture]
    public class ReplicationManagerTests
    {
        private readonly ConcurrentBag<IServerInformation> serverInformations = new ConcurrentBag<IServerInformation>();

        private ReplicationManager manager;

        private Mock<IRedisMultiplexer> multiplexer;

        private EndPoint server;

        [SetUp]
        public void Setup()
        {
            server = new IPEndPoint(IPAddress.Any, 6000);
            multiplexer = new Mock<IRedisMultiplexer>();
            manager = new ReplicationManager(multiplexer.Object, server, TimeSpan.FromMilliseconds(100));
            multiplexer.Setup(item => item.GetInfo(ReplicationInfo.Name)).Returns(serverInformations);
        }

        [Test]
        public void Close()
        {
            manager.Open();
            manager.Close();
            Assert.AreEqual(ChannelState.Closed, manager.State);
            multiplexer.Verify(item => item.SetupSlave(null));
        }

        [Test]
        public void Construct()
        {
            Assert.Throws<ArgumentNullException>(() => new ReplicationManager(null, server, TimeSpan.FromSeconds(1)));
            Assert.Throws<ArgumentNullException>(() => new ReplicationManager(multiplexer.Object, null, TimeSpan.FromSeconds(1)));
            Assert.AreEqual(server, manager.Master);
            Assert.AreEqual(ChannelState.New, manager.State);
        }

        [Test]
        public void Open()
        {
            manager.Open();
            Assert.AreEqual(ChannelState.Open, manager.State);
            multiplexer.Verify(item => item.SetupSlave(server));
        }

        [Test]
        public void VerifyReplicationProcess()
        {
            List<ReplicationEventArgs> arguments = new List<ReplicationEventArgs>();
            manager.StepCompleted += (sender, args) => arguments.Add(args);
            manager.Open();
            Thread.Sleep(300);
            Assert.AreEqual(0, arguments.Count);

            Mock<IServerInformation> information = new Mock<IServerInformation>();
            Mock<IReplicationInfo> replicationInfo = new Mock<IReplicationInfo>();
            information.Setup(item => item.Replication).Returns(replicationInfo.Object);
            information.Setup(item => item.Server).Returns(new IPEndPoint(IPAddress.Any, 1717));
            replicationInfo.Setup(item => item.IsMasterSyncInProgress).Returns(1);
            serverInformations.Add(information.Object);
            Thread.Sleep(300);
            Assert.AreEqual(0, arguments.Count);

            replicationInfo.Setup(item => item.IsMasterSyncInProgress).Returns(0);
            replicationInfo.Setup(item => item.SlaveReplOffset).Returns(10);
            Thread.Sleep(300);
            Assert.AreEqual(1, arguments.Count);

            replicationInfo.Setup(item => item.SlaveReplOffset).Returns(100);
            Thread.Sleep(300);
            Assert.AreEqual(2, arguments.Count);
        }
    }
}