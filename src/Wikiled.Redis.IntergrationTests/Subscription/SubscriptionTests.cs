﻿using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml.Linq;
using NLog;
using NUnit.Framework;
using Wikiled.Common.Serialization;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Config;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Serialization.Subscription;

namespace Wikiled.Redis.IntegrationTests.Subscription
{
    [TestFixture]
    public class SubscriptionTests
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private RedisInside.Redis redisInstance;

        private RedisLink redis;

        private ObjectKey key;

        [SetUp]
        public void Setup()
        {
            redisInstance = new RedisInside.Redis(i => i.Port(6666).LogTo(item => log.Debug(item)));
            var config = XDocument.Load(Path.Combine(TestContext.CurrentContext.TestDirectory, @"Config\redis.config")).XmlDeserialize<RedisConfiguration>();
            redis = new RedisLink("IT", new RedisMultiplexer(config));
            redis.Open();
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
        public void TestSubscribtion()
        {
            List<KeyspaceEvent> events = new List<KeyspaceEvent>();
            redis.SubscribeKeyEvents(key, @event => events.Add(@event));
            redis.Client.AddRecord(key, new Identity());
            Thread.Sleep(1000);
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual("rpush", (string)events[0].Value);
            Assert.AreEqual("IT:object:Test:Key", events[0].Key);
        }

        [Test]
        public void TestTypeSubscribtion()
        {
            List<KeyspaceEvent> events = new List<KeyspaceEvent>();
            redis.RegisterHashType<Identity>();
            redis.SubscribeTypeEvents<Identity>(@event => events.Add(@event));
            redis.Client.AddRecord(key, new Identity { ApplicationId = "Test" });
            Thread.Sleep(1000);
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual("hset", (string)events[0].Value);
        }
    }
}
