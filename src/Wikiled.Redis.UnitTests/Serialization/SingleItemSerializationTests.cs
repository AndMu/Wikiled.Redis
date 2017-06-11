using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Serialization;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Config;

namespace Wikiled.Redis.UnitTests.Serialization
{
    [TestFixture]
    public class SingleItemSerializationTests
    {
        private SingleItemSerialization instance;

        private Mock<IRedisLink> link;

        private Identity data;

        private ObjectKey key;

        private Mock<IDatabaseAsync> database;

        private Mock<IObjectSerialization> objecMock;

        [SetUp]
        public void Setup()
        {
            RedisConfiguration configuration = new RedisConfiguration();
            link = new Mock<IRedisLink>();
            var multiplexer = new Mock<IRedisMultiplexer>();
            multiplexer.Setup(item => item.Configuration).Returns(configuration);
            link.Setup(item => item.Multiplexer).Returns(multiplexer.Object);
            link.Setup(item => item.LinkId).Returns(0);
            link.Setup(item => item.State).Returns(ChannelState.Open);
            key = new ObjectKey("Test");
            data = new Identity();
            database = new Mock<IDatabaseAsync>();
            objecMock = new Mock<IObjectSerialization>();
            link.Setup(item => item.GetDefinition<Identity>()).Returns(HandlingDefinition<Identity>.ConstructGeneric(link.Object));
            instance = new SingleItemSerialization(link.Object, objecMock.Object);
        }

        [Test]
        public void Construct()
        {
            Assert.Throws<ArgumentNullException>(() => new SingleItemSerialization(null, objecMock.Object));
            Assert.Throws<ArgumentNullException>(() => new SingleItemSerialization(link.Object, null));
        }

        [Test]
        public async Task DeleteAll()
        {
            Assert.Throws<ArgumentNullException>(() => instance.DeleteAll(null, key));
            Assert.Throws<ArgumentNullException>(() => instance.DeleteAll(database.Object, null));
            await instance.DeleteAll(database.Object, key).ConfigureAwait(false);
            database.Verify(item => item.KeyDeleteAsync(It.IsAny<RedisKey>(), CommandFlags.None));
        }

        [Test]
        public async Task AddRecord()
        {
            Assert.Throws<ArgumentNullException>(() => instance.AddRecord(null, key, data));
            Assert.Throws<ArgumentNullException>(() => instance.AddRecord(database.Object, null, data));
            Assert.Throws<ArgumentNullException>(() => instance.AddRecord<Identity>(database.Object, key, null));
            await instance.AddRecord(database.Object, key, data).ConfigureAwait(false);
            database.Verify(item => item.HashSetAsync(It.IsAny<RedisKey>(), It.IsAny<HashEntry[]>(), CommandFlags.None));
        }

        [Test]
        public async Task ListIndex()
        {
            key.AddIndex(new IndexKey("Test", false));
            await instance.AddRecord(database.Object, key, data).ConfigureAwait(false);
            database.Verify(item => item.HashSetAsync(It.IsAny<RedisKey>(), It.IsAny<HashEntry[]>(), CommandFlags.None), Times.Exactly(1));
            database.Verify(item => item.ListLeftPushAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), When.Always, CommandFlags.None), Times.Exactly(1));
        }

        [Test]
        public async Task HashIndex()
        {
            key.AddIndex(new HashIndexKey("Test", "Test2"));
            await instance.AddRecord(database.Object, key, data).ConfigureAwait(false);
            database.Verify(item => item.HashSetAsync(It.IsAny<RedisKey>(), It.IsAny<HashEntry[]>(), CommandFlags.None), Times.Exactly(2));
            database.Verify(item => item.ListLeftPushAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), When.Always, CommandFlags.None), Times.Exactly(0));
        }

        [Test]
        public async Task GetRecords()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () => await instance.GetRecords<Identity>(database.Object, null));
            Assert.ThrowsAsync<ArgumentNullException>(async () => await instance.GetRecords<Identity>(null, key));
            objecMock.Setup(item => item.GetInstances<Identity>(It.IsAny<RedisValue[]>())).Returns(new[] { data });

            database.Setup(item => item.KeyExistsAsync(":object:Test", CommandFlags.None))
                    .Returns(Task.FromResult(true));
            database.Setup(
                item =>
                item.SortAsync(
                    ":object:Test",
                    0,
                    -1,
                    Order.Ascending,
                    SortType.Numeric,
                    "nosort",
                    It.IsAny<RedisValue[]>(),
                    CommandFlags.PreferMaster)).Returns(Task.FromResult(new RedisValue[] { }));
            var record = await instance.GetRecords<Identity>(database.Object, key).FirstAsync();
            Assert.AreSame(data, record);
        }

        [Test]
        public void GetRecordsRange()
        {
            Assert.Throws<ArgumentNullException>(() => instance.GetRecords<Identity>(database.Object, null, 0, 10));
            Assert.Throws<ArgumentNullException>(() => instance.GetRecords<Identity>(null, key, 0, 10));
        }
    }
}
