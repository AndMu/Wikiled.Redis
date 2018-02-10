using System.Configuration;
using System.IO;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using NLog;
using NUnit.Framework;
using Wikiled.Core.Utility.Serialization;
using Wikiled.Redis.Config;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Replication;

namespace Wikiled.Redis.IntegrationTests.Replication
{
    [TestFixture]
    public class RedisReplicationTests
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private RedisInside.Redis redisOne;

        private RedisInside.Redis redisTwo;

        private RedisLink linkOne;

        private RedisLink linkTwo;

        private string key = "TestData";

        private ReplicationFactory factory;

        [SetUp]
        public async Task Setup()
        {
            factory = new ReplicationFactory(new SimpleRedisFactory(), TaskPoolScheduler.Default);
            redisOne = new RedisInside.Redis(i => i.LogTo(item => log.Debug(item)));
            redisTwo = new RedisInside.Redis(i => i.LogTo(item => log.Debug(item)));
            

            await Task.Delay(1000).ConfigureAwait(false);
            var config = XDocument.Load(Path.Combine(TestContext.CurrentContext.TestDirectory, @"Config\redis.config")).XmlDeserialize<RedisConfiguration>();
            config.Endpoints[0].Port = ((IPEndPoint)redisOne.Endpoint).Port;
            linkOne = new RedisLink("RedisOne", new RedisMultiplexer(config));

            config = XDocument.Load(Path.Combine(TestContext.CurrentContext.TestDirectory, @"Config\redis.config")).XmlDeserialize<RedisConfiguration>();
            config.Endpoints[0].Port = ((IPEndPoint)redisTwo.Endpoint).Port;
            linkTwo = new RedisLink("RedisTwo", new RedisMultiplexer(config));
            linkOne.Open();
            linkOne.Multiplexer.Flush();
            Thread.Sleep(1000);
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
            var result = await factory.Replicate(new DnsEndPoint("localhost", 6017), new DnsEndPoint("localhost", 6027), new CancellationTokenSource(10000).Token).ConfigureAwait(false);
            ValidateResultOn(result);
            ValidateOff(1);
        }

        [Test]
        public async Task TestReplication()
        {
            int replicationWait = 10000;
            using (var replication = factory.StartReplicationFrom(linkOne.Multiplexer, linkTwo.Multiplexer))
            {
                CancellationTokenSource tokenSource = new CancellationTokenSource(replicationWait);
                var completed = await replication.Progress.Where(item => item.InSync)
                                                 .FirstAsync()
                                                 .ToTask(tokenSource.Token).ConfigureAwait(false);

                ValidateResultOn(completed);

                tokenSource = new CancellationTokenSource(replicationWait);
                await replication.Progress.Where(item => item.InSync && item.Master.Offset != completed.Master.Offset)
                                 .FirstAsync()
                                 .ToTask(tokenSource.Token).ConfigureAwait(false);

                // add data while in replication mode
                linkOne.Database.ListLeftPush(key, "Test2");

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

        private void ValidateResultOn(ReplicationProgress result)
        {
            var data = linkTwo.Database.ListRange(key);
            Assert.AreEqual(1, data.Length);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsActive);
            Assert.IsTrue(result.InSync);
        }
    }
}
