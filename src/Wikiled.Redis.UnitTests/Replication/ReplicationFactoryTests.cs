using Moq;
using NUnit.Framework;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;
using Wikiled.Redis.Config;
using Wikiled.Redis.Information;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Replication;

namespace Wikiled.Redis.UnitTests.Replication
{
    [TestFixture]
    public class ReplicationFactoryTests : ReactiveTest
    {
        private TestScheduler scheduler;

        private ReplicationFactory instance;

        private ReplicationTestManager testManager;

        private Func<IRedisConfiguration, IRedisMultiplexer> redisFactory;

        private int counter;

        [SetUp]
        public void Setup()
        {
            testManager = new ReplicationTestManager();

            redisFactory = configuration =>
            {
                var result = counter % 2 == 0 ? testManager.Master : testManager.Slave;
                counter++;

                return result.Object;
            };

            scheduler = new TestScheduler();
            instance = CreateFactory();
        }

        [Test]
        public void StartReplicationFromArguments()
        {
            Assert.Throws<ArgumentNullException>(() => instance.StartReplicationFrom(null, testManager.Slave.Object));
            Assert.Throws<ArgumentNullException>(() => instance.StartReplicationFrom(testManager.Master.Object, null));
        }

        [Test]
        public void StartReplicationFrom()
        {
            testManager.SetupReplication();
            var replication = instance.StartReplicationFrom(testManager.Master.Object, testManager.Slave.Object);
            var observer = scheduler.CreateObserver<ReplicationProgress>();
            replication.Progress.Subscribe(observer);
            var ticks = TimeSpan.FromSeconds(1).Ticks;
            scheduler.AdvanceBy(ticks);
            observer.Messages.AssertEqual(OnNext<ReplicationProgress>(ticks, progress => !progress.IsActive));
        }

        [Test]
        public void Replicate()
        {
            var master = new DnsEndPoint("Master", 1);
            var slave = new DnsEndPoint("Slave", 2);
            var replicationInfo = SetupReplication();
            var task = instance.Replicate(master, slave, CancellationToken.None);

            var ticks = TimeSpan.FromSeconds(1).Ticks;
            scheduler.AdvanceBy(ticks);
            Thread.Sleep(100);
            Assert.IsFalse(task.IsCompleted);

            replicationInfo.Setup(item => item.Slaves).Returns(new[] { SlaveInformation.Parse("ip=127.0.0.1,port=6666,state=online,offset=2000,lag=0") });
            scheduler.AdvanceBy(ticks);
            Thread.Sleep(100);

            Assert.IsTrue(task.IsCompleted);
        }

        [Test]
        public void ReplicateCancel()
        {
            var master = new DnsEndPoint("Master", 1);
            var slave = new DnsEndPoint("Slave", 2);
            SetupReplication();
            CancellationTokenSource source = new CancellationTokenSource();
            var task = instance.Replicate(master, slave, source.Token);
            source.Cancel();
            Assert.ThrowsAsync<TaskCanceledException>(() => task);
        }

        private Mock<IReplicationInfo> SetupReplication()
        {
            var replicationInfo = testManager.SetupReplication();
            replicationInfo.Setup(item => item.MasterReplOffset).Returns(2000);
            return replicationInfo;
        }

        [Test]
        public void Construct()
        {
            Assert.Throws<ArgumentNullException>(() => new ReplicationFactory(null, scheduler));
            Assert.Throws<ArgumentNullException>(() => new ReplicationFactory(redisFactory, null));
        }

        private ReplicationFactory CreateFactory()
        {
            return new ReplicationFactory(redisFactory, scheduler);
        }
    }
}
