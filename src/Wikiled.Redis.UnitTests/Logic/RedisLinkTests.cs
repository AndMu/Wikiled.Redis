using System;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Config;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Serialization;
using Wikiled.Redis.Serialization.Subscription;

namespace Wikiled.Redis.UnitTests.Logic
{
    [TestFixture]
    public class RedisLinkTests
    {
        private Mock<IRedisMultiplexer> multiplexer;

        private Mock<IDatabase> database;

        private RedisLink redisLink;

        [SetUp]
        public void Setup()
        {
            RedisConfiguration configuration = new RedisConfiguration("Test");
            database = new Mock<IDatabase>();
            multiplexer = new Mock<IRedisMultiplexer>();
            multiplexer.Setup(item => item.Database).Returns(database.Object);
            multiplexer.Setup(item => item.Configuration).Returns(configuration);
            redisLink = new RedisLink("Redis", multiplexer.Object);
            redisLink.Open();
        }

        [Test]
        public void SubscribeKeyEvents()
        {
            ObjectKey key = new ObjectKey("Test", "Id");
            Assert.Throws<ArgumentNullException>(() => redisLink.SubscribeKeyEvents(null, @event => Console.WriteLine(@event.Key)));
            Assert.Throws<ArgumentNullException>(() => redisLink.SubscribeKeyEvents(key, null));
            Mock<ISubscriber> subscriber = new Mock<ISubscriber>();
            multiplexer.Setup(item => item.SubscribeKeyEvents("Redis:object:Test:Id", It.IsAny<Action<KeyspaceEvent>>()))
                       .Returns(subscriber.Object);
            var result = redisLink.SubscribeKeyEvents(key, @event => Console.WriteLine(@event.Key));
            Assert.AreEqual(subscriber.Object, result);
        }

        [Test]
        public void SubscribeTypeEvents()
        {
            redisLink.RegisterHashType<Identity>();
            Assert.Throws<ArgumentNullException>(() => redisLink.SubscribeTypeEvents<Identity>(null));
            Mock<ISubscriber> subscriber = new Mock<ISubscriber>();
            multiplexer.Setup(item => item.SubscribeKeyEvents("Redis:object:Identity:*", It.IsAny<Action<KeyspaceEvent>>()))
                       .Returns(subscriber.Object);
            var result = redisLink.SubscribeTypeEvents<Identity>(@event => Console.WriteLine(@event.Key));
            Assert.AreEqual(subscriber.Object, result);
        }

        [Test]
        public void SubscribeTypeEventsNotSupported()
        {
            var result = redisLink.SubscribeTypeEvents<Identity>(@event => Console.WriteLine(@event.Key));
            Assert.IsNull(result);
        }

        [Test]
        public void DefaultSerialization()
        {
            var specificClient = redisLink.GetSpecific<Identity>();
            Assert.IsInstanceOf<ListSerialization>(specificClient);
        }

        [Test]
        public void GetSpecificNormalized()
        {
            redisLink.RegisterNormalized<Identity>();
            var specificClient = redisLink.GetSpecific<Identity>();
            Assert.IsInstanceOf<ObjectListSerialization>(specificClient);
        }

        [Test]
        public void GetSpecificNormalizedSingle()
        {
            redisLink.RegisterNormalized<Identity>().IsSingleInstance = true;
            var specificClient = redisLink.GetSpecific<Identity>();
            Assert.IsInstanceOf<SingleItemSerialization>(specificClient);
        }

        [Test]
        public void GetSpecificHash()
        {
            redisLink.RegisterHashType<Identity>();
            var specificClient = redisLink.GetSpecific<Identity>();
            Assert.IsInstanceOf<ObjectListSerialization>(specificClient);
        }

        [Test]
        public void GetSpecificHashSingle()
        {
            redisLink.RegisterHashType<Identity>().IsSingleInstance = true;
            var specificClient = redisLink.GetSpecific<Identity>();
            Assert.IsInstanceOf<SingleItemSerialization>(specificClient);
        }

        [Test]
        public void GetTypeIDExist()
        {
            database.Setup(item => item.SetMembers("Redis:Type:Identity", CommandFlags.None)).Returns(new RedisValue[] { "One" });
            var id = redisLink.GetTypeID(typeof(Identity));
            Assert.AreEqual("One", id);
            var type = redisLink.GetTypeByName(id);
            Assert.AreEqual(typeof(Identity), type);
        }

        [Test]
        public void GetTypeIDNew()
        {
            Mock<IBatch> batch = new Mock<IBatch>();
            database.Setup(item => item.CreateBatch(null)).Returns(batch.Object);
            database.Setup(item => item.StringIncrement("Redis:Type:Counter", 1, CommandFlags.None)).Returns(2);
            var id = redisLink.GetTypeID(typeof(Identity));
            Assert.AreEqual("Type:2", id);
            var type = redisLink.GetTypeByName(id);
            Assert.AreEqual(typeof(Identity), type);
            batch.Verify(
                item => item.SetAddAsync("Redis:Type:2", "Wikiled.Redis.Channels.Identity,Wikiled.Redis", CommandFlags.PreferMaster));
            batch.Verify(item => item.SetAddAsync("Redis:Type:Identity", "Type:2", CommandFlags.PreferMaster));
            batch.Verify(item => item.Execute());
        }

        [Test]
        public void Create()
        {
            Assert.Throws<ArgumentNullException>(() => new RedisLink("Redis", null));
            Assert.AreEqual("Redis", redisLink.Name);
            Assert.AreEqual(multiplexer.Object, redisLink.Multiplexer);
            Assert.NotNull(redisLink.Generator);
        }

        [Test]
        public void GetDefinitionUnknown()
        {
            var definition = redisLink.GetDefinition<Identity>();
            Assert.IsNotNull(definition);
            Assert.IsFalse(definition.IsWellKnown);
        }

        [Test]
        public void RegisterDefinition()
        {
            var result = redisLink.HasDefinition<Identity>();
            Assert.IsFalse(result);
            redisLink.RegisterNormalized<Identity>();
            result = redisLink.HasDefinition<Identity>();
            Assert.IsTrue(result);
            var definition = redisLink.GetDefinition<Identity>();
            Assert.IsNotNull(definition);
            Assert.IsTrue(definition.IsNormalized);
        }

        [Test]
        public void RegisterHashType()
        {
            redisLink.RegisterHashType<Identity>();
            var definition = redisLink.GetDefinition<Identity>();
            Assert.IsNotNull(definition);
            Assert.IsTrue(definition.IsWellKnown);
            Assert.AreEqual("L0:1", definition.GetNextId());
        }

        [Test]
        public void Open()
        {
            redisLink = new RedisLink("Redis", multiplexer.Object);
            redisLink.Open();
            multiplexer.Verify(item => item.Open());
        }

        [Test]
        public void OpenFailed()
        {
            redisLink = new RedisLink("Redis", multiplexer.Object);
            multiplexer.Setup(item => item.Open()).Throws(new Exception());
            Assert.Throws<Exception>(() => redisLink.Open());
            Assert.AreEqual(ChannelState.Closed, redisLink.State);
            multiplexer.Setup(item => item.Open());
            redisLink.Open();
            Assert.AreEqual(ChannelState.Open, redisLink.State);
        }

        [Test]
        public void Close()
        {
            redisLink.Close();
            multiplexer.Verify(item => item.Close());
        }

        [Test]
        public void Dispose()
        {
            redisLink.Dispose();
            multiplexer.Verify(item => item.Dispose());
        }
    }
}
