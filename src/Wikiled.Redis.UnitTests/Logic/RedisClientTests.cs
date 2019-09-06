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
using Wikiled.Redis.Indexing;

namespace Wikiled.Redis.UnitTests.Logic
{
    [TestFixture]
    public class RedisClientTests
    {
        private RedisClient client;

        private Mock<IDatabase> database;

        private ObjectKey key;

        private Mock<IRedisLink> link;

        private Mock<ISpecificPersistency> persistency;

        private Mock<IMainIndexManager> mainIndexManager;

        private Identity data;

        [SetUp]
        public void Setup()
        {
            mainIndexManager = new Mock<IMainIndexManager>();
            link = new Mock<IRedisLink>();
            persistency = new Mock<ISpecificPersistency>();
            link.Setup(item => item.GetSpecific<Identity>()).Returns(persistency.Object);
            database = new Mock<IDatabase>();
            link.Setup(item => item.Database).Returns(database.Object);
            key = new ObjectKey("Name");
            client = new RedisClient(link.Object, mainIndexManager.Object);
            data = new Identity();
        }

        [Test]
        public void Construct()
        {
            Assert.Throws<ArgumentNullException>(() => new RedisClient(null, mainIndexManager.Object));
            Assert.Throws<ArgumentNullException>(() => new RedisClient(link.Object, null));
        }

        [Test]
        public async Task AddRecord()
        {
            Assert.Throws<ArgumentNullException>(() => client.AddRecord(null, new Identity()));
            Assert.Throws<ArgumentNullException>(() => client.AddRecord<Identity>(key, null));
            await client.AddRecord(key, data).ConfigureAwait(false);
            persistency.Verify(item => item.AddRecord(database.Object, key, data));
        }

        [Test]
        public async Task TestLink()
        {
            var local = new Mock<IDatabaseAsync>();
            client = new RedisClient(link.Object, mainIndexManager.Object, local.Object);
            await client.AddRecord(key, data).ConfigureAwait(false);
            persistency.Verify(item => item.AddRecord(local.Object, key, data));
        }

        [Test]
        public async Task AddRecords()
        {
            var keys = new[] { key };
            Assert.ThrowsAsync<ArgumentNullException>(async () => await client.AddRecords(null, new Identity()).ConfigureAwait(false));
            Assert.ThrowsAsync<ArgumentNullException>(async () => await client.AddRecords<Identity>(keys, null).ConfigureAwait(false));
            await client.AddRecords(keys, data).ConfigureAwait(false);
            persistency.Verify(item => item.AddRecords(database.Object, keys, data));
        }

        [Test]
        public async Task GetRecords()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () => await client.GetRecords<Identity>(null));
            persistency.Setup(item => item.GetRecords<Identity>(database.Object, key, 0, -1)).Returns(new[] {new Identity()}.ToObservable());
            var result = await client.GetRecords<Identity>(key).LastOrDefaultAsync();
            Assert.IsNotNull(result);
        }
    }
}