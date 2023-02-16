using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Information;
using Wikiled.Redis.Replication;

namespace Wikiled.Redis.UnitTests.Replication
{
    [TestFixture]
    public class ReplicationManagerTests
    {
        private ReplicationTestManager testManager;

        private ReplicationManager manager;

        private Subject<long> timer;

        [SetUp]
        public void Setup()
        {
            timer = new Subject<long>();
            testManager = new ReplicationTestManager();
            manager = new ReplicationManager(new NullLogger<ReplicationManager>(), testManager.Master.Object, testManager.Slave.Object, timer);
        }

        [Test]
        public async Task Close()
        {
            await manager.Open();
            manager.Close();
            Assert.AreEqual(ChannelState.Closed, manager.State);
            testManager.Slave.Verify(item => item.SetupSlave(null));
        }

        [Test]
        public void Construct()
        {
            Assert.Throws<ArgumentNullException>(() => new ReplicationManager(new NullLogger<ReplicationManager>(), null, testManager.Slave.Object, timer));
            Assert.Throws<ArgumentNullException>(() => new ReplicationManager(new NullLogger<ReplicationManager>(), testManager.Master.Object, null, timer));
            Assert.Throws<ArgumentNullException>(() => new ReplicationManager(new NullLogger<ReplicationManager>(), testManager.Master.Object, testManager.Slave.Object, null));
            Assert.Throws<ArgumentNullException>(() => new ReplicationManager(null, testManager.Master.Object, testManager.Slave.Object, timer));
        }

        [Test]
        public async Task Open()
        {
            await manager.Open();
            Assert.AreEqual(ChannelState.Open, manager.State);
            testManager.Slave.Verify(item => item.SetupSlave(It.IsAny<EndPoint>()));
        }

        [Test]
        public void OpenMasterDown()
        {
            testManager.Master.Setup(item => item.IsActive).Returns(false);
            Assert.ThrowsAsync<InvalidOperationException>(manager.Open);
        }

        [Test]
        public void OpenSlaveDown()
        {
            testManager.Slave.Setup(item => item.IsActive).Returns(false);
            Assert.ThrowsAsync<InvalidOperationException>(manager.Open);
        }

        [Test]
        public void OpenMasterZero()
        {
            testManager.Master.Setup(item => item.GetServers()).Returns(new IServer[] { });
            Assert.ThrowsAsync<InvalidOperationException>(manager.Open);
        }

        [Test]
        public async Task VerifyReplicationProcess()
        {
            var replicationInfo = testManager.SetupReplication();
            var arguments = new List<ReplicationProgress>();
            manager.Progress.Subscribe(
                item =>
                {
                    arguments.Add(item);
                });
            await manager.Open();

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
        public async Task VerifyReplicationProcessError()
        {
            var arguments = new List<ReplicationProgress>();
            manager.Progress.Subscribe(
                item =>
                {
                    arguments.Add(item);
                });
            await manager.Open();

            Assert.AreEqual(0, arguments.Count);
            Assert.Throws<InvalidOperationException>(() => timer.OnNext(1));
        }
    }
}
