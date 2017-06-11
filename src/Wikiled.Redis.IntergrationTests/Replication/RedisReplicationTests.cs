using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Xml.Linq;
using NUnit.Framework;
using Wikiled.Core.Utility.Serialization;
using Wikiled.Redis.Config;
using Wikiled.Redis.Information;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Replication;

namespace Wikiled.Redis.IntegrationTests.Replication
{
    [TestFixture]
    public class RedisReplicationTests
    {
        private RedisProcessManager redisOne;

        private RedisProcessManager redisTwo;

        private RedisLink linkOne;

        private RedisLink linkTwo;

        [SetUp]
        public void Setup()
        {
            redisOne = new RedisProcessManager(6017);
            redisTwo = new RedisProcessManager(6027);
            redisOne.Start(TestContext.CurrentContext.TestDirectory);
            redisTwo.Start(TestContext.CurrentContext.TestDirectory);

            var config = XDocument.Load(Path.Combine(TestContext.CurrentContext.TestDirectory, @"Config\redis.config")).XmlDeserialize<RedisConfiguration>();
            config.Endpoints[0].Port = 6017;
            linkOne = new RedisLink("RedisOne", new RedisMultiplexer(config));

            config = XDocument.Load(Path.Combine(TestContext.CurrentContext.TestDirectory, @"Config\redis.config")).XmlDeserialize<RedisConfiguration>();
            config.Endpoints[0].Port = 6027;
            linkTwo = new RedisLink("RedisTwo", new RedisMultiplexer(config));

            linkOne.Open();
            linkOne.Multiplexer.Flush();
            linkTwo.Open();
            linkTwo.Multiplexer.Flush();
        }

        [TearDown]
        public void TearDown()
        {
            redisOne.Dispose();
            redisTwo.Dispose();
            linkTwo.Dispose();
            linkOne.Dispose();
        }

        [Test]
        public void TestReplication()
        {
            int replicationWait = 10000;
            var data = linkOne.Database.ListRange("TestData");
            Assert.AreEqual(0, data.Length);

            // adding new recorrd
            linkOne.Database.ListLeftPush("TestData", "Test");
            data = linkOne.Database.ListRange("TestData");
            Assert.AreEqual(1, data.Length);

            // checking nothing in another database
            data = linkTwo.Database.ListRange("TestData");
            Assert.AreEqual(0, data.Length);

            using (var replication = linkTwo.SetupReplication(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6017)))
            {
                ManualResetEvent syncEvent = new ManualResetEvent(false);
                ReplicationEventArgs argument = null;
                replication.StepCompleted += (sender, args) =>
                    {
                        argument = args;
                        syncEvent.Set();
                    };
                replication.Open();
                if (!syncEvent.WaitOne(replicationWait))
                {
                    throw new TimeoutException("Replication timeout");
                }

                data = linkTwo.Database.ListRange("TestData");
                Assert.AreEqual(1, data.Length);

                Assert.IsNotNull(argument);
                Assert.AreEqual(ReplicationRole.Slave, argument.Status.Role);
                Assert.AreEqual(MasterLinkStatus.Up, argument.Status.MasterLinkStatus);
                Assert.GreaterOrEqual(argument.Status.LastSync, 0);
                Assert.Greater(argument.Status.SlaveReplOffset, 0);

                syncEvent.Reset();

                // add data while in replication mode
                linkOne.Database.ListLeftPush("TestData", "Test2");
                if (!syncEvent.WaitOne(replicationWait))
                {
                    throw new TimeoutException("Replication timeout");
                }

                data = linkTwo.Database.ListRange("TestData");
                Assert.AreEqual(2, data.Length);
                replication.Close();
            }

            // add data out of replication mode
            linkOne.Database.ListLeftPush("TestData", "Test3");
            Thread.Sleep(1000);
            data = linkTwo.Database.ListRange("TestData");
            Assert.AreEqual(2, data.Length);

            data = linkOne.Database.ListRange("TestData");
            Assert.AreEqual(3, data.Length);
        }
    }
}
