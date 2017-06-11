﻿using System;
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
    public class ObjectListSerializationTests
    {
        private ObjectListSerialization instance;

        private ObjectKey key;

        private Mock<IRedisLink> link;

        private Mock<IDatabaseAsync> database;

        private Mock<IObjectSerialization> objecMock;

        private Identity data;

        private Mock<IRedisSetList> redisSetList;

        [SetUp]
        public void Setup()
        {
            RedisConfiguration configuration = new RedisConfiguration();
            link = new Mock<IRedisLink>();
            var multiplexer = new Mock<IRedisMultiplexer>();
            link.Setup(item => item.Multiplexer).Returns(multiplexer.Object);
            multiplexer.Setup(item => item.Configuration).Returns(configuration);
            redisSetList = new Mock<IRedisSetList>();
            link.Setup(item => item.State).Returns(ChannelState.Open);
            link.Setup(item => item.LinkId).Returns(0);
            objecMock = new Mock<IObjectSerialization>();
            link.Setup(item => item.GetDefinition<Identity>()).Returns(HandlingDefinition<Identity>.ConstructGeneric(link.Object));
            database = new Mock<IDatabaseAsync>();
            key = new ObjectKey("Test");
            data = new Identity();
            instance = new ObjectListSerialization(link.Object, objecMock.Object, redisSetList.Object);
        }

        [Test]
        public void Construct()
        {
            Assert.Throws<ArgumentNullException>(() => new ObjectListSerialization(null, objecMock.Object, redisSetList.Object));
            Assert.Throws<ArgumentNullException>(() => new ObjectListSerialization(link.Object, null, redisSetList.Object));
            Assert.Throws<ArgumentNullException>(() => new ObjectListSerialization(link.Object, objecMock.Object, null));
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
        }

        [Test]
        public async Task AddRecord()
        {
            Assert.Throws<ArgumentNullException>(() => instance.AddRecord(null, key, data));
            Assert.Throws<ArgumentNullException>(() => instance.AddRecord(database.Object, null, data));
            Assert.Throws<ArgumentNullException>(() => instance.AddRecord<Identity>(database.Object, key, null));
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
            database.Verify(item => item.HashSetAsync(It.IsAny<RedisKey>(), It.IsAny<HashEntry[]>(), CommandFlags.None), Times.Exactly(2));
            redisSetList.Verify(item => item.SaveItems(database.Object, It.IsAny<IDataKey>(), It.IsAny<RedisValue[]>()));
        }

        [Test]
        public async Task GetRecords()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () => await instance.GetRecords<Identity>(database.Object, null));
            Assert.ThrowsAsync<ArgumentNullException>(async () => await instance.GetRecords<Identity>(null, key));
            objecMock.Setup(item => item.GetColumns<Identity>()).Returns(new[] { "Test" });
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
                    CommandFlags.PreferMaster))
                    .Returns(Task.FromResult(new RedisValue[] { }));
            var record = await instance.GetRecords<Identity>(database.Object, key).FirstAsync();
            Assert.AreSame(data, record);
        }

        [Test]
        public async Task GetRecordsRange()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () => await instance.GetRecords<Identity>(database.Object, null, 0, 10));
            Assert.ThrowsAsync<ArgumentNullException>(async () => await instance.GetRecords<Identity>(null, key, 0, 10));

            objecMock.Setup(item => item.GetColumns<Identity>()).Returns(new[] { "Test" });
            objecMock.Setup(item => item.GetInstances<Identity>(It.IsAny<RedisValue[]>())).Returns(new[] { data });

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
            var record = await instance.GetRecords<Identity>(database.Object, key, 1, 10).FirstAsync();
            Assert.AreSame(data, record);
        }
    }
}
