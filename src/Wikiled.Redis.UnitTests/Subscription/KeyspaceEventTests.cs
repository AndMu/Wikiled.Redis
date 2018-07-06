using System;
using NUnit.Framework;
using StackExchange.Redis;
using Wikiled.Redis.Serialization.Subscription;

namespace Wikiled.Redis.UnitTests.Subscription
{
    [TestFixture]
    public class KeyspaceEventTests
    {
        [Test]
        public void Construct()
        {
            Assert.Throws<ArgumentException>(() => new KeyspaceEvent(null, new RedisChannel("Test", RedisChannel.PatternMode.Auto), "Test"));
            Assert.Throws<ArgumentException>(() => new KeyspaceEvent("Test", new RedisChannel(string.Empty, RedisChannel.PatternMode.Auto), "Test"));
            Assert.Throws<ArgumentException>(() => new KeyspaceEvent("Test", new RedisChannel("Test", RedisChannel.PatternMode.Auto), string.Empty));
            KeyspaceEvent keyEvent = new KeyspaceEvent("Test", new RedisChannel("Test2", RedisChannel.PatternMode.Auto), "Raw");
            Assert.AreEqual("Test", keyEvent.Key);
        }
    }
}
