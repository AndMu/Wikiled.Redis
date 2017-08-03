using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Reactive.Subjects;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Information;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Replication;

namespace Wikiled.Redis.UnitTests.Replication
{
    [TestFixture]
    public class ReplicationManagerTests
    {
        private IPEndPoint clientAddress;

        private ReplicationManager manager;

        private Mock<IRedisMultiplexer> master;

        private Mock<IRedisMultiplexer> slave;

        private ConcurrentBag<IServerInformation> serverInformations;

        private Subject<long> timer;

        [SetUp]
        public void Setup()
        {
            serverInformations = new ConcurrentBag<IServerInformation>();
            clientAddress = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6666);
            master = new Mock<IRedisMultiplexer>();
            master.Setup(item => item.IsActive).Returns(true);
            slave = new Mock<IRedisMultiplexer>();
            slave.Setup(item => item.IsActive).Returns(true);
            var client = new Mock<IServer>();
            client.Setup(item => item.EndPoint).Returns(clientAddress);
            slave.Setup(item => item.GetServers()).Returns(new[] {client.Object});
            master.Setup(item => item.GetServers()).Returns(new[] { client.Object });
            timer = new Subject<long>();

            manager = new ReplicationManager(master.Object, slave.Object, timer);
            master.Setup(item => item.GetInfo(ReplicationInfo.Name)).Returns(serverInformations);
        }

        [Test]
        public void Close()
        {
            manager.Open();
            manager.Close();
            Assert.AreEqual(ChannelState.Closed, manager.State);
            slave.Verify(item => item.SetupSlave(null));
        }

        [Test]
        public void Construct()
        {
            Assert.Throws<ArgumentNullException>(() => new ReplicationManager(null, slave.Object, timer));
            Assert.Throws<ArgumentNullException>(() => new ReplicationManager(master.Object, null, timer));
            Assert.Throws<ArgumentNullException>(() => new ReplicationManager(master.Object, slave.Object, null));
        }

        [Test]
        public void Open()
        {
            manager.Open();
            Assert.AreEqual(ChannelState.Open, manager.State);
            slave.Verify(item => item.SetupSlave(It.IsAny<EndPoint>()));
        }

        [Test]
        public void VerifyReplicationProcess()
        {
            var replicationInfo = SetupReplication();
            List<ReplicationProgress> arguments = new List<ReplicationProgress>();
            manager.Progress.Subscribe(
                item =>
                {
                    arguments.Add(item);
                });
            manager.Open();
            
            Assert.AreEqual(0, arguments.Count);
            timer.OnNext(1);

            Assert.IsFalse(arguments[0].IsActive);
            replicationInfo.Setup(item => item.MasterReplOffset).Returns(2000);
            replicationInfo.Setup(item => item.Slaves).Returns(new[] { SlaveInformation.Parse("ip=127.0.0.1,port=6666,state=online,offset=239,lag=0") });

            timer.OnNext(1);
            Assert.AreEqual(2, arguments.Count);
            Assert.IsTrue(arguments[1].IsActive);
            Assert.IsFalse(arguments[1].InSync);

            replicationInfo.Setup(item => item.Slaves).Returns(new[] { SlaveInformation.Parse("ip=127.0.0.1,port=6666,state=online,offset=2000,lag=0") });
            timer.OnNext(1);
            Assert.AreEqual(3, arguments.Count);
            Assert.IsTrue(arguments[2].IsActive);
            Assert.IsTrue(arguments[2].InSync);
        }

        [Test]
        public void VerifyReplicationProcessError()
        {
            List<ReplicationProgress> arguments = new List<ReplicationProgress>();
            manager.Progress.Subscribe(
                item =>
                {
                    arguments.Add(item);
                });
            manager.Open();

            Assert.AreEqual(0, arguments.Count);
            Assert.Throws<InvalidOperationException>(() => timer.OnNext(1));
        }

        private Mock<IReplicationInfo> SetupReplication()
        {
            Mock<IServerInformation> information = new Mock<IServerInformation>();
            Mock<IReplicationInfo> replicationInfo = new Mock<IReplicationInfo>();
            information.Setup(item => item.Replication).Returns(replicationInfo.Object);
            information.Setup(item => item.Server).Returns(new IPEndPoint(IPAddress.Any, 1717));
            serverInformations.Add(information.Object);
            replicationInfo.Setup(item => item.Role).Returns(ReplicationRole.Master);
            return replicationInfo;
        }
    }
}
