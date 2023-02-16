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
    public class SingleItemSerializationTests
    {
        private SingleItemSerialization<Identity> instance;

        private Mock<IRedisLink> link;

        private Identity data;

        private ObjectKey key;

        private Mock<IDatabaseAsync> database;

        private Mock<IObjectSerialization<Identity>> objecMock;

        private Mock<IMainIndexManager> mainIndexManager;

        [SetUp]
        public void Setup()
        {
            mainIndexManager = new Mock<IMainIndexManager>();
            var configuration = new RedisConfiguration("Test");
            link = new Mock<IRedisLink>();
            link.Setup(item => item.Resilience).Returns(new ResilienceHandler(new NullLogger<ResilienceHandler>(), new ResilienceConfig()));
            var multiplexer = new Mock<IRedisMultiplexer>();
            multiplexer.Setup(item => item.Configuration).Returns(configuration);
            link.Setup(item => item.Multiplexer).Returns(multiplexer.Object);
            link.Setup(item => item.LinkId).Returns(0);
            link.Setup(item => item.State).Returns(ChannelState.Open);
            key = new ObjectKey("Test");
            data = new Identity();
            database = new Mock<IDatabaseAsync>();
            objecMock = new Mock<IObjectSerialization<Identity>>();
            instance = new SingleItemSerialization<Identity>(new NullLogger<SingleItemSerialization<Identity>>(), link.Object, objecMock.Object, mainIndexManager.Object);
        }

        [Test]
        public void Construct()
        {
            ConstructorHelper.ConstructorMustThrowArgumentNullException<SingleItemSerialization<Identity>>();
        }

        [Test]
        public async Task DeleteAll()
        {
            Assert.Throws<ArgumentNullException>(() => instance.DeleteAll(null, key));
            Assert.Throws<ArgumentNullException>(() => instance.DeleteAll(database.Object, null));
            await instance.DeleteAll(database.Object, key);
            database.Verify(item => item.KeyDeleteAsync(It.IsAny<RedisKey>(), CommandFlags.None));
        }

        [Test]
        public async Task AddRecord()
        {
            Assert.Throws<ArgumentNullException>(() => instance.AddRecord(null, key, data));
            Assert.Throws<ArgumentNullException>(() => instance.AddRecord(database.Object, null, data));
            Assert.Throws<ArgumentNullException>(() => instance.AddRecord(database.Object, key, null));
            await instance.AddRecord(database.Object, key, data);
            database.Verify(item => item.HashSetAsync(It.IsAny<RedisKey>(), It.IsAny<HashEntry[]>(), CommandFlags.None));
        }

        [Test]
        public async Task ListIndex()
        {
            key.AddIndex(new IndexKey("Test"));
            await instance.AddRecord(database.Object, key, data);
            database.Verify(item => item.HashSetAsync(It.IsAny<RedisKey>(), It.IsAny<HashEntry[]>(), CommandFlags.None), Times.Exactly(1));
            mainIndexManager.Verify(item => item.Add(It.IsAny<IDatabaseAsync>(), It.IsAny<IDataKey>()));
        }

        [Test]
        public async Task HashIndex()
        {
            key.AddIndex(new HashIndexKey("Test", "Test2"));
            await instance.AddRecord(database.Object, key, data);
            mainIndexManager.Verify(item => item.Add(It.IsAny<IDatabaseAsync>(), It.IsAny<IDataKey>()));
            database.Verify(item => item.HashSetAsync(It.IsAny<RedisKey>(), It.IsAny<HashEntry[]>(), CommandFlags.None));
            database.Verify(item => item.ListLeftPushAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), When.Always, CommandFlags.None), Times.Exactly(0));
        }

        [Test]
        public async Task GetRecords()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () => await instance.GetRecords(database.Object, null));
            Assert.ThrowsAsync<ArgumentNullException>(async () => await instance.GetRecords(null, key));
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
                    CommandFlags.PreferMaster)).Returns(Task.FromResult(new RedisValue[] { }));
            var record = await instance.GetRecords(database.Object, key).FirstAsync();
            Assert.AreSame(data, record);
        }

        [Test]
        public void GetRecordsRange()
        {
            Assert.Throws<ArgumentNullException>(() => instance.GetRecords(database.Object, null, 0, 10));
            Assert.Throws<ArgumentNullException>(() => instance.GetRecords(null, key, 0, 10));
        }
    }
}
