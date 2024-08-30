using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Serialization;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using StackExchange.Redis;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Indexing;
using Wikiled.Redis.Logic.Resilience;

namespace Wikiled.Redis.UnitTests.Logic
{
    [TestFixture]
    public class RedisClientTests
    {
        private RedisClient client;

        private Mock<IDatabase> database;

        private ObjectKey key;

        private Mock<IRedisLink> link;

        private Mock<ISpecificPersistency<Identity>> persistency;

        private Mock<IMainIndexManager> mainIndexManager;

        private Identity data;

        [SetUp]
        public void Setup()
        {
            mainIndexManager = new Mock<IMainIndexManager>();
            link = new Mock<IRedisLink>();
            link.Setup(item => item.Resilience).Returns(new ResilienceHandler(new NullLogger<ResilienceHandler>(), new ResilienceConfig()));
            persistency = new Mock<ISpecificPersistency<Identity>>();
            link.Setup(item => item.GetSpecific<Identity>()).Returns(persistency.Object);
            database = new Mock<IDatabase>();
            link.Setup(item => item.Database).Returns(database.Object);
            key = new ObjectKey("Name");
            client = new RedisClient(new NullLogger<RedisClient>(), link.Object, mainIndexManager.Object);
            data = new Identity();
        }

        [Test]
        public void Construct()
        {
            ClassicAssert.Throws<ArgumentNullException>(() => new RedisClient(new NullLogger<RedisClient>(), null, mainIndexManager.Object));
            ClassicAssert.Throws<ArgumentNullException>(() => new RedisClient(new NullLogger<RedisClient>(), link.Object, null));
            ClassicAssert.Throws<ArgumentNullException>(() => new RedisClient(null, link.Object, mainIndexManager.Object));
        }

        [Test]
        public async Task AddRecord()
        {
            ClassicAssert.Throws<ArgumentNullException>(() => client.AddRecord(null, new Identity()));
            ClassicAssert.Throws<ArgumentNullException>(() => client.AddRecord<Identity>(key, null));
            await client.AddRecord(key, data);
            persistency.Verify(item => item.AddRecord(database.Object, key, data));
        }

        [Test]
        public async Task TestLink()
        {
            var local = new Mock<IDatabaseAsync>();
            client = new RedisClient(new NullLogger<RedisClient>(), link.Object, mainIndexManager.Object, local.Object);
            await client.AddRecord(key, data);
            persistency.Verify(item => item.AddRecord(local.Object, key, data));
        }

        [Test]
        public async Task AddRecords()
        {
            var keys = new[] { key };
            ClassicAssert.ThrowsAsync<ArgumentNullException>(async () => await client.AddRecords(null, new Identity()));
            ClassicAssert.ThrowsAsync<ArgumentNullException>(async () => await client.AddRecords<Identity>(keys, null));
            await client.AddRecords(keys, data);
            persistency.Verify(item => item.AddRecords(database.Object, keys, data));
        }

        [Test]
        public async Task GetRecords()
        {
            ClassicAssert.ThrowsAsync<ArgumentNullException>(async () => await client.GetRecords<Identity>(null));
            persistency.Setup(item => item.GetRecords(database.Object, key, 0, -1)).Returns(new[] {new Identity()}.ToObservable());
            var result = await client.GetRecords<Identity>(key).LastOrDefaultAsync();
            ClassicAssert.IsNotNull(result);
        }
    }
}