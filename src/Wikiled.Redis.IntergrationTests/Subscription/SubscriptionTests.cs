using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Wikiled.Common.Serialization;
using Wikiled.Common.Utilities.Modules;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Config;
using Wikiled.Redis.IntegrationTests.Helpers;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Serialization.Subscription;

namespace Wikiled.Redis.IntegrationTests.Subscription
{
    [TestFixture]
    public class SubscriptionTests
    {
        private RedisInside.Redis redisInstance;

        private IRedisLink redis;

        private ObjectKey key;

        [SetUp]
        public async Task Setup()
        {
            redisInstance = new RedisInside.Redis(i => i.Port(6666).LogTo(item => Global.Logger.LogDebug(item)));
            var config = XDocument.Load(Path.Combine(TestContext.CurrentContext.TestDirectory, @"Config\redis.config")).XmlDeserialize<RedisConfiguration>();
            config.ServiceName = "IT";
            redis = await new ModuleHelper(config).Provider.GetService<IAsyncServiceFactory<IRedisLink>>().GetService(true);
            redis.Multiplexer.Flush();
            key = new ObjectKey("Test", "Key");
        }

        [TearDown]
        public void TearDown()
        {
            redis.Dispose();
            redisInstance.Dispose();
        }

        [Test]
        public void TestSubstitution()
        {
            var events = new List<KeyspaceEvent>();
            redis.SubscribeKeyEvents(key, @event => events.Add(@event));
            redis.Client.AddRecord(key, new Identity());
            Thread.Sleep(1000);
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual("rpush", (string)events[0].Value);
            Assert.AreEqual("IT:object:Test:Key", events[0].Key);
        }

        [Test]
        public void TestTypeSubscription()
        {
            var events = new List<KeyspaceEvent>();
            redis.PersistencyRegistration.RegisterHashsetList<Identity>();
            redis.SubscribeTypeEvents<Identity>(@event => events.Add(@event));
            redis.Client.AddRecord(key, new Identity { ApplicationId = "Test" });
            Thread.Sleep(1000);
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual("hset", (string)events[0].Value);
        }
    }
}
