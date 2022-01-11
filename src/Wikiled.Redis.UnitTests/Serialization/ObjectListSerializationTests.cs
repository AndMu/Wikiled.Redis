using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Serialization;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;
using Wikiled.Common.Testing.Utilities.Reflection;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Config;
using Wikiled.Redis.Indexing;
using Wikiled.Redis.Logic.Resilience;

namespace Wikiled.Redis.UnitTests.Serialization
{
    [TestFixture]
    public class ObjectListSerializationTests
    {
        private ObjectListSerialization<Identity> instance;

        private ObjectKey key;

        private Mock<IRedisLink> link;

        private Mock<IDatabaseAsync> database;

        private Mock<IObjectSerialization<Identity>> objecMock;

        private Identity data;

        private Mock<IRedisSetList> redisSetList;

        private Mock<IMainIndexManager> mainIndexManager;

        [SetUp]
        public void Setup()
        {
            mainIndexManager = new Mock<IMainIndexManager>();
            var configuration = new RedisConfiguration("Test");
            link = new Mock<IRedisLink>();
            link.Setup(item => item.Resilience).Returns(new ResilienceHandler(new NullLogger<ResilienceHandler>(), new ResilienceConfig()));
            var multiplexer = new Mock<IRedisMultiplexer>();
            link.Setup(item => item.Multiplexer).Returns(multiplexer.Object);
            multiplexer.Setup(item => item.Configuration).Returns(configuration);
            redisSetList = new Mock<IRedisSetList>();
            link.Setup(item => item.State).Returns(ChannelState.Open);
            link.Setup(item => item.LinkId).Returns(0);
            objecMock = new Mock<IObjectSerialization<Identity>>();
            database = new Mock<IDatabaseAsync>();
            key = new ObjectKey("Test");
            data = new Identity();
            instance = new ObjectListSerialization<Identity>(new NullLogger<ObjectListSerialization<Identity>>(), link.Object, objecMock.Object, redisSetList.Object, mainIndexManager.Object);
        }

        [Test]
        public void Construct()
        {
            ConstructorHelper.ConstructorMustThrowArgumentNullException<ObjectListSerialization<Identity>>();
        }

        [Test]
        public async Task DeleteAll()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () => await instance.DeleteAll(null, key).ConfigureAwait(false));
            Assert.ThrowsAsync<ArgumentNullException>(async () => await instance.DeleteAll(database.Object, null).ConfigureAwait(false));

            redisSetList.Setup(
                item =>
                item.GetRedisValues(It.IsAny<IDatabaseAsync>(), It.IsAny<RedisKey>(), 0, -1))
                    .Returns(Task.FromResult(new RedisValue[] { "Test1", "Test2" }));

            database.Setup(item => item.KeyDeleteAsync(new[] { (RedisKey)"Test1", (RedisKey)"Test2", (RedisKey)":object:Test" }, CommandFlags.None))
                    .Returns(Task.FromResult(0L));

            await instance.DeleteAll(database.Object, key).ConfigureAwait(false);
            database.Verify(item => item.KeyDeleteAsync(new[] { (RedisKey)"Test1", (RedisKey)"Test2", (RedisKey)":object:Test" }, CommandFlags.None));

            mainIndexManager.Verify(item => item.Delete(It.IsAny<IDatabaseAsync>(), It.IsAny<IDataKey>()));
        }

        [Test]
        public async Task AddRecord()
        {
            Assert.Throws<ArgumentNullException>(() => instance.AddRecord(null, key, data));
            Assert.Throws<ArgumentNullException>(() => instance.AddRecord(database.Object, null, data));
            Assert.Throws<ArgumentNullException>(() => instance.AddRecord(database.Object, key, null));
            await instance.AddRecord(database.Object, key, data).ConfigureAwait(false);
            database.Verify(item => item.HashSetAsync(It.IsAny<RedisKey>(), It.IsAny<HashEntry[]>(), CommandFlags.None));
            redisSetList.Verify(
                item => item.SaveItems(database.Object, It.IsAny<IDataKey>(), It.IsAny<RedisValue[]>()));
        }

        [Test]
        public async Task ListIndex()
        {
            key.AddIndex(new IndexKey("Test", false));
            await instance.AddRecord(database.Object, key, data).ConfigureAwait(false);
            database.Verify(item => item.HashSetAsync(It.IsAny<RedisKey>(), It.IsAny<HashEntry[]>(), CommandFlags.None), Times.Exactly(1));
            redisSetList.Verify(item => item.SaveItems(database.Object, It.IsAny<IDataKey>(), It.IsAny<RedisValue[]>()));
        }

        [Test]
        public async Task HashIndex()
        {
            key.AddIndex(new HashIndexKey("Test", "Test2"));
            await instance.AddRecord(database.Object, key, data).ConfigureAwait(false);
            database.Verify(item => item.HashSetAsync(It.IsAny<RedisKey>(), It.IsAny<HashEntry[]>(), CommandFlags.None));
            redisSetList.Verify(item => item.SaveItems(database.Object, It.IsAny<IDataKey>(), It.IsAny<RedisValue[]>()));
        }

        [Test]
        public async Task GetRecords()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () => await instance.GetRecords(database.Object, null));
            Assert.ThrowsAsync<ArgumentNullException>(async () => await instance.GetRecords(null, key));
            objecMock.Setup(item => item.GetColumns()).Returns(new[] { "Test" });
            objecMock.Setup(item => item.GetInstances(It.IsAny<RedisValue[]>())).Returns(new[] { data });

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
                    CommandFlags.PreferMaster))
                    .Returns(Task.FromResult(new RedisValue[] { }));
            var record = await instance.GetRecords(database.Object, key).FirstAsync();
            Assert.AreSame(data, record);
        }

        [Test]
        public async Task GetRecordsRange()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () => await instance.GetRecords(database.Object, null, 0, 10));
            Assert.ThrowsAsync<ArgumentNullException>(async () => await instance.GetRecords(null, key, 0, 10));

            objecMock.Setup(item => item.GetColumns()).Returns(new[] { "Test" });
            objecMock.Setup(item => item.GetInstances(It.IsAny<RedisValue[]>())).Returns(new[] { data });

            database.Setup(item => item.KeyExistsAsync(":object:Test", CommandFlags.None))
                    .Returns(Task.FromResult(true));

            database.Setup(
                item =>
                item.SortAsync(
                    ":object:Test",
                    1,
                    10,
                    Order.Ascending,
                    SortType.Numeric,
                    "nosort",
                    It.IsAny<RedisValue[]>(),
                    CommandFlags.PreferMaster)).Returns(Task.FromResult(new RedisValue[] { }));
            var record = await instance.GetRecords(database.Object, key, 1, 10).FirstAsync();
            Assert.AreSame(data, record);
        }
    }
}
