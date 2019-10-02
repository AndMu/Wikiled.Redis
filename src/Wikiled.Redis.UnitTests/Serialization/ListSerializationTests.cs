﻿using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Scripts;
using Wikiled.Redis.Serialization;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Data;
using Wikiled.Redis.Indexing;
using Wikiled.Redis.Logic.Resilience;
using Wikiled.Redis.UnitTests.MockData;

namespace Wikiled.Redis.UnitTests.Serialization
{
    [TestFixture]
    public class ListSerializationTests
    {
        private Mock<IRedisLink> redis;

        private Mock<IDatabaseAsync> database;

        private ListSerialization instance;

        private ObjectKey key;

        private MainDataOne data;

        private Mock<IDataSerializer> serializer;

        private Mock<IRedisSetList> redisSetList;

        private Mock<IMainIndexManager> indexManager;

        [SetUp]
        public void Setup()
        {
            data = new MainDataOne();
            indexManager = new Mock<IMainIndexManager>();
            redisSetList = new Mock<IRedisSetList>();
            key = new ObjectKey("Test");
            redis = new Mock<IRedisLink>();
            redis.Setup(item => item.Resilience).Returns(new ResilienceHandler(new NullLogger<ResilienceHandler>(), new ResilienceConfig()));
            redis.Setup(item => item.State).Returns(ChannelState.Open);
            redis.Setup(item => item.LinkId).Returns(0);
            redis.Setup(item => item.Generator).Returns(new ScriptGenerator());
            serializer = new Mock<IDataSerializer>();
            redis.Setup(item => item.GetDefinition<MainDataOne>()).Returns(Global.HandlingDefinitionFactory.ConstructGeneric<MainDataOne>(redis.Object, serializer.Object));
            database = new Mock<IDatabaseAsync>();
            instance = new ListSerialization(new NullLogger<ListSerialization>(), redis.Object, redisSetList.Object, indexManager.Object);
        }

        [Test]
        public void Construct()
        {
            Assert.Throws<ArgumentNullException>(() => new ListSerialization(new NullLogger<ListSerialization>(), null, redisSetList.Object, indexManager.Object));
            Assert.Throws<ArgumentNullException>(() => new ListSerialization(new NullLogger<ListSerialization>(), redis.Object, null, indexManager.Object));
            Assert.Throws<ArgumentNullException>(() => new ListSerialization(new NullLogger<ListSerialization>(), redis.Object, redisSetList.Object, null));
            Assert.Throws<ArgumentNullException>(() => new ListSerialization(null, redis.Object, redisSetList.Object, indexManager.Object));
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
            Assert.Throws<ArgumentNullException>(() => instance.AddRecord(database.Object, null, data));
            Assert.Throws<ArgumentNullException>(() => instance.AddRecord<Identity>(database.Object, key, null));
            Assert.Throws<ArgumentNullException>(() => instance.AddRecord(null, key, data));
            await instance.AddRecord(database.Object, key, data).ConfigureAwait(false);
            redisSetList.Verify(item => item.SaveItems(database.Object, It.IsAny<IDataKey>(), It.IsAny<RedisValue>()));
        }

        [Test]
        public async Task GetRecords()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () => await instance.GetRecords<MainDataOne>(database.Object, null));
            Assert.ThrowsAsync<ArgumentNullException>(async () => await instance.GetRecords<MainDataOne>(null, key));

            var dataInstance = new MainDataOne();
            dataInstance.Name = "Test";
            serializer.Setup(item => item.Deserialize<MainDataOne>(It.IsAny<byte[]>())).Returns(dataInstance);
            redisSetList.Setup(
                item =>
                item.GetRedisValues(It.IsAny<IDatabaseAsync>(), It.IsAny<RedisKey>(), 0, -1))
                    .Returns(Task.FromResult(new RedisValue[] { new byte[] { 1 } }));

            var records = await instance.GetRecords<MainDataOne>(database.Object, key).ToArray();
            Assert.AreEqual(1, records.Count());
        }

        [Test]
        public async Task GetRecordsRange()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () => await instance.GetRecords<MainDataOne>(database.Object, null, 1, 10));
            Assert.ThrowsAsync<ArgumentNullException>(async () => await instance.GetRecords<MainDataOne>(null, key, 1, 10));
            database.Setup(
                item =>
                item.ScriptEvaluateAsync(
                    It.IsAny<string>(),
                    It.IsAny<RedisKey[]>(),
                    It.IsAny<RedisValue[]>(),
                    CommandFlags.PreferMaster)).Returns(Task.FromResult((RedisResult)null));
            await instance.GetRecords<Identity>(database.Object, key, 1, 10).ToArray();
        }
    }
}
