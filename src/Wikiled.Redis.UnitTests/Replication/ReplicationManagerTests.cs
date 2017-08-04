﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive.Subjects;
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
            manager = new ReplicationManager(testManager.Master.Object, testManager.Slave.Object, timer);
        }

        [Test]
        public void Close()
        {
            manager.Open();
            manager.Close();
            Assert.AreEqual(ChannelState.Closed, manager.State);
            testManager.Slave.Verify(item => item.SetupSlave(null));
        }

        [Test]
        public void Construct()
        {
            Assert.Throws<ArgumentNullException>(() => new ReplicationManager(null, testManager.Slave.Object, timer));
            Assert.Throws<ArgumentNullException>(() => new ReplicationManager(testManager.Master.Object, null, timer));
            Assert.Throws<ArgumentNullException>(() => new ReplicationManager(testManager.Master.Object, testManager.Slave.Object, null));
        }

        [Test]
        public void Open()
        {
            manager.Open();
            Assert.AreEqual(ChannelState.Open, manager.State);
            testManager.Slave.Verify(item => item.SetupSlave(It.IsAny<EndPoint>()));
        }

        [Test]
        public void OpenMasterDown()
        {
            testManager.Master.Setup(item => item.IsActive).Returns(false);
            Assert.Throws<InvalidOperationException>(() => manager.Open());
        }

        [Test]
        public void OpenSlaveDown()
        {
            testManager.Slave.Setup(item => item.IsActive).Returns(false);
            Assert.Throws<InvalidOperationException>(() => manager.Open());
        }

        [Test]
        public void OpenMasterZero()
        {
            testManager.Master.Setup(item => item.GetServers()).Returns(new IServer[] { });
            Assert.Throws<InvalidOperationException>(() => manager.Open());
        }

        [Test]
        public void VerifyReplicationProcess()
        {
            var replicationInfo = testManager.SetupReplication();
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
    }
}
