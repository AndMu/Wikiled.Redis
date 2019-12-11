using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Config;
using Wikiled.Redis.Data;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Logic.Resilience;
using Wikiled.Redis.Persistency;
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

        private RedisConfiguration configuration;

        private Mock<IResilience> resilience;

        private Mock<IEntitySubscriber> entitySubscriber;

        private Mock<IDataSerializer> defaultSerialiser;

        [SetUp]
        public async Task Setup()
        {
            defaultSerialiser = new Mock<IDataSerializer>();
            configuration = new RedisConfiguration("Test");
            resilience = new Mock<IResilience>();
            configuration.ServiceName = "Redis";
            database = new Mock<IDatabase>();
            multiplexer = new Mock<IRedisMultiplexer>();
            multiplexer.Setup(item => item.Database).Returns(database.Object);
            multiplexer.Setup(item => item.Configuration).Returns(configuration);
            entitySubscriber = new Mock<IEntitySubscriber>();
            redisLink = new RedisLink(new NullLoggerFactory(), configuration, multiplexer.Object, resilience.Object, entitySubscriber.Object, defaultSerialiser.Object);
            await redisLink.Open().ConfigureAwait(false);
        }

        [Test]
        public void SubscribeKeyEvents()
        {
            var key = new ObjectKey("Test", "Id");
            Assert.Throws<ArgumentNullException>(() => redisLink.SubscribeKeyEvents(null, @event => Console.WriteLine(@event.Key)));
            Assert.Throws<ArgumentNullException>(() => redisLink.SubscribeKeyEvents(key, null));
            var subscriber = new Mock<ISubscriber>();
            multiplexer.Setup(item => item.SubscribeKeyEvents("Redis:object:Test:Id", It.IsAny<Action<KeyspaceEvent>>()))
                       .Returns(subscriber.Object);
            var result = redisLink.SubscribeKeyEvents(key, @event => Console.WriteLine(@event.Key));
            Assert.AreEqual(subscriber.Object, result);
        }

        [Test]
        public void SubscribeTypeEvents()
        {
            redisLink.PersistencyRegistration.RegisterHashsetList<Identity>();
            Assert.Throws<ArgumentNullException>(() => redisLink.SubscribeTypeEvents<Identity>(null));
            var subscriber = new Mock<ISubscriber>();
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
            Assert.IsInstanceOf<ListSerialization<Identity>>(specificClient);
        }

        [Test]
        public void RegisterHashsetList()
        {
            redisLink.PersistencyRegistration.RegisterHashsetList<Identity>();
            var specificClient = redisLink.GetSpecific<Identity>();
            Assert.IsInstanceOf<ObjectListSerialization<Identity>>(specificClient);
        }

        [Test]
        public void GetSpecificNormalizedSingle()
        {
            redisLink.PersistencyRegistration.RegisterHashsetSingle<Identity>();
            var specificClient = redisLink.GetSpecific<Identity>();
            Assert.IsInstanceOf<SingleItemSerialization<Identity>>(specificClient);
        }

        [Test]
        public void RegisterObjectHashList()
        {
            redisLink.PersistencyRegistration.RegisterObjectHashList<Identity>(new BinaryDataSerializer());
            var specificClient = redisLink.GetSpecific<Identity>();
            Assert.IsInstanceOf<ObjectListSerialization<Identity>>(specificClient);
        }

        [Test]
        public void GetSpecificHashSingle()
        {
            redisLink.PersistencyRegistration.RegisterObjectHashSingle<Identity>(new BinaryDataSerializer());
            var specificClient = redisLink.GetSpecific<Identity>();
            Assert.IsInstanceOf<SingleItemSerialization<Identity>>(specificClient);
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
            var batch = new Mock<IBatch>();
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
            Assert.Throws<ArgumentNullException>(() => new RedisLink(new NullLoggerFactory(), configuration, null, resilience.Object, entitySubscriber.Object, defaultSerialiser.Object));
            Assert.AreEqual("Redis", redisLink.Name);
            Assert.AreEqual(multiplexer.Object, redisLink.Multiplexer);
            Assert.NotNull(redisLink.Generator);
        }
      
        [Test]
        public async Task Open()
        {
            redisLink = new RedisLink(new NullLoggerFactory(), configuration, multiplexer.Object,  resilience.Object, entitySubscriber.Object, defaultSerialiser.Object);
            await redisLink.Open().ConfigureAwait(false);
            multiplexer.Verify(item => item.Open());
        }

        [Test]
        public void OpenRetry()
        {
            multiplexer = new Mock<IRedisMultiplexer>();
            multiplexer.Setup(item => item.Open()).Throws(new Exception());
            redisLink = new RedisLink(new NullLoggerFactory(), configuration, multiplexer.Object,  resilience.Object, entitySubscriber.Object, defaultSerialiser.Object);
            Assert.ThrowsAsync<Exception>(redisLink.Open);
            Assert.ThrowsAsync<Exception>(redisLink.Open);
            multiplexer.Verify(item => item.Open(), Times.Exactly(2));
            multiplexer.Verify(item => item.Close(), Times.Exactly(2));
        }

        [Test]
        public async Task OpenFailed()
        {
            redisLink = new RedisLink(new NullLoggerFactory(), configuration, multiplexer.Object, resilience.Object, entitySubscriber.Object, defaultSerialiser.Object);
            multiplexer.Setup(item => item.Open()).Throws(new Exception());
            Assert.ThrowsAsync<Exception>(redisLink.Open);
            Assert.AreEqual(ChannelState.Closed, redisLink.State);
            multiplexer.Setup(item => item.Open());
            await redisLink.Open().ConfigureAwait(false);
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
