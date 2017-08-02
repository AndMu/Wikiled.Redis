using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
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

        private string key = "TestData";

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

            var data = linkOne.Database.ListRange(key);
            Assert.AreEqual(0, data.Length);

            // adding new recorrd
            linkOne.Database.ListLeftPush(key, "Test");
            data = linkOne.Database.ListRange(key);
            Assert.AreEqual(1, data.Length);

            // checking nothing in another database
            data = linkTwo.Database.ListRange(key);
            Assert.AreEqual(0, data.Length);
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
        public async Task TestReplicationAsync()
        {
            using (var replication = linkTwo.SetupReplicationFrom(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6017)))
            {
                var result = await replication.Perform(new CancellationTokenSource(10000).Token);
                ValidateResultOn(result);
                ValidateOff(1);
            }
        }

        [Test]
        public void TestReplication()
        {
            int replicationWait = 1000000;
            using (var replication = linkTwo.SetupReplicationFrom(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6017)))
            {
                ManualResetEvent syncEvent = new ManualResetEvent(false);
                ReplicationEventArgs argument = null;
                replication.OnSynchronized += (sender, args) =>
                    {
                        argument = args;
                        syncEvent.Set();
                    };
                replication.Open();
                if (!syncEvent.WaitOne(replicationWait))
                {
                    throw new TimeoutException("Replication timeout");
                }

                ValidateResultOn(argument.Status);
                syncEvent.Reset();

                // add data while in replication mode
                linkOne.Database.ListLeftPush(key, "Test2");
                if (!syncEvent.WaitOne(replicationWait))
                {
                    throw new TimeoutException("Replication timeout");
                }

                var result = linkTwo.Database.ListRange(key);
                Assert.AreEqual(2, result.Length);
                replication.Close();
            }

            ValidateOff(2);
        }

        private void ValidateOff(int total)
        {
            // add data out of replication mode
            linkOne.Database.ListLeftPush(key, "Test3");
            Thread.Sleep(1000);
            var data = linkTwo.Database.ListRange(key);
            Assert.AreEqual(total, data.Length);

            data = linkOne.Database.ListRange(key);
            Assert.AreEqual(total + 1, data.Length);
        }

        private void ValidateResultOn(IReplicationInfo result)
        {
            var data = linkTwo.Database.ListRange(key);
            Assert.AreEqual(1, data.Length);

            Assert.IsNotNull(result);
            Assert.AreEqual(ReplicationRole.Master, result.Role);
        }
    }
}
