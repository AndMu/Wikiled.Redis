using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Wikiled.Redis.Information;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Replication;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Config;

namespace Wikiled.Redis.UnitTests.Replication
{
    [TestFixture]
    public class ReplicationManagerTests
    {
        private ConcurrentBag<IServerInformation> serverInformations;

        private ReplicationManager manager;

        private Mock<IRedisMultiplexer> multiplexer;

        private Mock<IRedisMultiplexer> master;

        private Mock<IRedisFactory> factory;

        private IPEndPoint server;

        private IPEndPoint clientAddress;

        [SetUp]
        public void Setup()
        {
            serverInformations = new ConcurrentBag<IServerInformation>();
            clientAddress = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6666);
            master = new Mock<IRedisMultiplexer>();
            master.Setup(item => item.IsActive).Returns(true);
            factory = new Mock<IRedisFactory>();
            factory.Setup(item => item.Create(It.IsAny<RedisConfiguration>())).Returns(master.Object);
            server = new IPEndPoint(IPAddress.Any, 6000);
            multiplexer = new Mock<IRedisMultiplexer>();
            var client = new Mock<IServer>();
            client.Setup(item => item.EndPoint).Returns(clientAddress);
            multiplexer.Setup(item => item.GetServers()).Returns(new[] { client.Object });
            manager = new ReplicationManager(factory.Object, server, multiplexer.Object, TimeSpan.FromMilliseconds(100));
            master.Setup(item => item.GetInfo(ReplicationInfo.Name)).Returns(serverInformations);
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
            Assert.Throws<ArgumentNullException>(() => new ReplicationManager(null, server, multiplexer.Object, TimeSpan.FromSeconds(1)));
            Assert.Throws<ArgumentNullException>(() => new ReplicationManager(factory.Object, server, null, TimeSpan.FromSeconds(1)));
            Assert.Throws<ArgumentNullException>(() => new ReplicationManager(factory.Object, null, multiplexer.Object, TimeSpan.FromSeconds(1)));
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
            manager.OnCompleted += (sender, args) => arguments.Add(args);

            var replicationInfo = SetupReplication();
            manager.Open();
            Thread.Sleep(300);
            Assert.AreEqual(0, arguments.Count);

            replicationInfo.Setup(item => item.MasterReplOffset).Returns(2000);
            replicationInfo.Setup(item => item.Slaves).Returns(new[] { SlaveInformation.Parse("ip=127.0.0.1,port=6666,state=online,offset=239,lag=0") });

            Thread.Sleep(300);
            Assert.AreEqual(0, arguments.Count);

            replicationInfo.Setup(item => item.Slaves).Returns(new[] { SlaveInformation.Parse("ip=127.0.0.1,port=6666,state=online,offset=2000,lag=0") });
            Thread.Sleep(300);
            Assert.AreEqual(1, arguments.Count);
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

        [Test]
        public void VerifyReplicationProcessError()
        {
            bool isError = false;
            bool isCompleted = false;
            manager.OnError += (sender, args) => { isError = true; };
            manager.OnCompleted += (sender, args) => { isCompleted = true; };

            manager.Open();
            Thread.Sleep(300);
            Assert.IsTrue(isError);
            Assert.IsFalse(isCompleted);
        }

        [Test]
        public async Task Perform()
        {
            var replicationInfo = SetupReplication();
            replicationInfo.Setup(item => item.MasterReplOffset).Returns(2000);
            replicationInfo.Setup(item => item.Slaves).Returns(new[] { SlaveInformation.Parse("ip=127.0.0.1,port=6666,state=online,offset=2000,lag=0") });
            var result = await manager.Perform(CancellationToken.None);
            Assert.IsNotNull(result);
        }

        [Test]
        public void PerformError()
        {
            Assert.ThrowsAsync<TaskCanceledException>(() => manager.Perform(CancellationToken.None));
        }
    }
}