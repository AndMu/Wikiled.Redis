using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using System.Xml.Linq;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;
using Wikiled.Core.Utility.Serialization;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Config;
using Wikiled.Redis.Helpers;
using Wikiled.Redis.Indexing;
using Wikiled.Redis.IntegrationTests.MockData;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Persistency;
using Wikiled.Redis.Serialization;

namespace Wikiled.Redis.IntegrationTests.Persistency
{
    [TestFixture]
    public class RedisPersistencyTests
    {
        private ObjectKey key;

        private RedisLink redis;

        private Mock<ILimitedSizeRepository> repository;

        private RepositoryKey repositoryKey;

        private Identity routing;

        private IndexKey listAll;

        private IndexKey listAll2;

        [SetUp]
        public void Setup()
        {
            var config = XDocument.Load(Path.Combine(TestContext.CurrentContext.TestDirectory, @"Config\redis.config")).XmlDeserialize<RedisConfiguration>();
            redis = new RedisLink("IT", new RedisMultiplexer(config));
            redis.Open();
            redis.Multiplexer.Flush();

            var redis2 = new RedisLink("IT", new RedisMultiplexer(config));
            redis2.Open();
            key = new ObjectKey("Key1");
            routing = new Identity();
            routing.ApplicationId = "Test";
            routing.Environment = "DEV";
            repository = new Mock<ILimitedSizeRepository>();
            repository.Setup(item => item.Name).Returns("Test");
            repository.Setup(item => item.Size).Returns(2);
            repositoryKey = new RepositoryKey(repository.Object, key);
            listAll = new IndexKey(repository.Object, "All", true);
            listAll2 = new IndexKey(repository.Object, "All2", true);
        }

        [TearDown]
        public void TearDown()
        {
            redis.Dispose();
        }

        [Test]
        public async Task AddToLimitedList()
        {
            var result = await redis.Client.ContainsRecord<string>(repositoryKey).ConfigureAwait(false);
            Assert.IsFalse(result);
            await redis.Client.AddRecord(repositoryKey, "Test1").ConfigureAwait(false);
            result = await redis.Client.ContainsRecord<string>(repositoryKey).ConfigureAwait(false);
            Assert.IsTrue(result);
            await redis.Client.AddRecord(repositoryKey, "Test2").ConfigureAwait(false);
            await redis.Client.AddRecord(repositoryKey, "Test3").ConfigureAwait(false);
            var value = await redis.Client.GetRecords<string>(repositoryKey).ToArray();
            Assert.AreEqual(2, value.Length);
            Assert.AreEqual("Test2", value[0]);
            Assert.AreEqual("Test3", value[1]);
        }

        [TestCase(1000)]
        [TestCase(2)]
        public async Task GetHash(int total)
        {
            for (int i = 0; i < total; i++)
            {
                redis.Database.HashSet("Test", i, i);
            }

            var result = await redis.GetHash("Test").ToArray();
            Assert.AreEqual(total, result.Length);
        }

        [Test]
        public async Task Primitive()
        {
            await redis.Client.AddRecord(repositoryKey, 1).ConfigureAwait(false);
            var result = await redis.Client.GetRecords<int>(repositoryKey).ToArray().ToTask().ConfigureAwait(false);
            Assert.AreEqual(1, result[0]);

            await redis.Client.AddRecord(repositoryKey, "1").ConfigureAwait(false);
            var resultString = await redis.Client.GetRecords<string>(repositoryKey).ToArray().ToTask().ConfigureAwait(false);
            Assert.AreEqual("1", resultString[0]);
        }

        [Test]
        public void PrimitiveType()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => redis.RegisterWellknown<string>());
        }

        [TestCase(true, 10)]
        [TestCase(true, 1)]
        [TestCase(false, 10)]
        [TestCase(false, 1)]
        public async Task TestIndex(bool useSets, int batchSize)
        {
            redis.Client.BatchSize = batchSize;
            var definition = redis.RegisterWellknown<Identity>();
            definition.IsSingleInstance = true;
            definition.IsSet = useSets;
            var key1 = new RepositoryKey(repository.Object, new ObjectKey("Test1"));
            key1.AddIndex(listAll);
            key1.AddIndex(listAll2);
            await redis.Client.AddRecord(key1, new Identity { Environment = "Test1" }).ConfigureAwait(false);

            var key2 = new RepositoryKey(repository.Object, new ObjectKey("Test2"));
            key2.AddIndex(listAll);
            key2.AddIndex(listAll2);
            await redis.Client.AddRecord(key2, new Identity { Environment = "Test2" }).ConfigureAwait(false);

            var key3 = new RepositoryKey(repository.Object, new ObjectKey("Test3"));
            key3.AddIndex(listAll);
            await redis.Client.AddRecord(key3, new Identity { Environment = "Test3" }).ConfigureAwait(false);

            var items = redis.Client.GetRecords<Identity>(listAll).ToEnumerable().OrderBy(item => item.Environment).ToArray();
            Assert.AreEqual(3, items.Length);
            Assert.AreEqual("Test1", items[0].Environment);
            Assert.AreEqual("Test2", items[1].Environment);
            Assert.AreEqual("Test3", items[2].Environment);

            items = redis.Client.GetRecords<Identity>(new[] { listAll, listAll2 }).ToEnumerable().OrderBy(item => item.Environment).ToArray();
            Assert.AreEqual(2, items.Length);
            Assert.AreEqual("Test1", items[0].Environment);
            Assert.AreEqual("Test2", items[1].Environment);

            items = await redis.Client.GetRecords<Identity>(listAll, 0, 0).ToArray();
            Assert.AreEqual(1, items.Length);
            Assert.AreEqual("Test3", items[0].Environment);

            items = redis.Client.GetRecords<Identity>(listAll, 1, 3).ToEnumerable().OrderBy(item => item.Environment).ToArray();
            Assert.AreEqual(2, items.Length);
            Assert.AreEqual("Test1", items[0].Environment);
            Assert.AreEqual("Test2", items[1].Environment);

            items = await redis.Client.GetRecords<Identity>(listAll, 3, 4).ToArray();
            Assert.AreEqual(0, items.Length);

            IndexManagerFactory factory = new IndexManagerFactory(redis, redis.Database);
            var manager = factory.Create(listAll);
            var count = await manager.Count().ConfigureAwait(false);
            Assert.AreEqual(3, count);

            count = (await manager.GetIds().ToArray()).Length;
            Assert.AreEqual(3, count);

            var result = await manager.GetIds(1, 2).ToArray();
            Assert.AreEqual("Test2", (string)result[0]);
            result = await manager.GetIds(0, 1).ToArray();
            Assert.AreEqual("Test3", (string)result[0]);

            await manager.Reset().ConfigureAwait(false);
            count = await manager.Count().ConfigureAwait(false);
            Assert.AreEqual(0, count);

            await redis.Reindex(key2).ConfigureAwait(false);
            count = await manager.Count().ConfigureAwait(false);
            Assert.AreEqual(3, count);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task TestIdentity(bool useSets)
        {
            var definition = redis.RegisterHashType<Identity>();
            definition.IsSingleInstance = true;
            definition.IsSet = useSets;

            var key1 = new RepositoryKey(repository.Object, new ObjectKey("Test1"));
            key1.AddIndex(listAll);

            var items = redis.Client.GetRecords<Identity>(listAll).ToEnumerable().ToArray();
            Assert.AreEqual(0, items.Length);

            Identity identity = new Identity();
            identity.Environment = "Ev";
            await redis.Client.AddRecord(key1, identity).ConfigureAwait(false);

            var key2 = new RepositoryKey(repository.Object, new ObjectKey("Test2"));
            key2.AddIndex(listAll);
            await redis.Client.AddRecord(key2, identity).ConfigureAwait(false);

            items = redis.Client.GetRecords<Identity>(listAll).ToEnumerable().ToArray();
            Assert.AreEqual(2, items.Length);
            Assert.AreEqual("Ev", items[0].Environment);
        }

        [Test]
        public async Task SaveSortedList()
        {
            await redis.Client.AddRecord(repositoryKey, new SortedSetEntry("One", 1))
                       .ConfigureAwait(false);
            await redis.Client.AddRecord(repositoryKey, new SortedSetEntry("Two", 2))
                       .ConfigureAwait(false);
            await redis.Client.AddRecord(repositoryKey, new SortedSetEntry("Fine", -2))
                       .ConfigureAwait(false);
            var result = await redis.Client.GetRecords<SortedSetEntry>(repositoryKey).ToArray();
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual("Fine", (string)result[0].Element);
            Assert.AreEqual("One", (string)result[1].Element);
            Assert.AreEqual("Two", (string)result[2].Element);
        }

        [Test]
        public async Task AddToLimitedListTransaction()
        {
            var transaction = redis.StartTransaction();
            var result = await redis.Client.ContainsRecord<string>(repositoryKey).ConfigureAwait(false);
            Assert.IsFalse(result);
            var task1 = transaction.Client.AddRecord(repositoryKey, "Test1");
            var task2 = transaction.Client.AddRecord(repositoryKey, "Test2");
            var task3 = transaction.Client.AddRecord(repositoryKey, "Test3");
            await transaction.Commit().ConfigureAwait(false);
            await Task.WhenAll(task1, task2, task3).ConfigureAwait(false);
            result = await redis.Client.ContainsRecord<string>(repositoryKey).ConfigureAwait(false);
            Assert.IsTrue(result);
            var value = await redis.Client.GetRecords<string>(repositoryKey).ToArray();
            Assert.AreEqual(2, value.Length);
            Assert.AreEqual("Test2", value[0]);
            Assert.AreEqual("Test3", value[1]);
        }

        [Test]
        public async Task SaveDictionary()
        {
            redis.RegisterHashType(new DictionarySerializer(new[] { "Result" }));
            Dictionary<string, string> table = new Dictionary<string, string>();
            table["Result"] = "one";
            await redis.Client.AddRecord(key, table).ConfigureAwait(false);
            var records = await redis.Client.GetRecords<Dictionary<string, string>>(key).ToArray();
            Assert.AreEqual(1, records.Length);
            Assert.AreEqual("one", records[0]["Result"]);
        }

        [Test]
        public async Task DeleteKeys()
        {
            var result = await redis.Client.ContainsRecord<string>(key).ConfigureAwait(false);
            Assert.IsFalse(result);
            var client = redis.Client;

            await client.AddRecord(key, "Test").ConfigureAwait(false);
            result = await redis.Client.ContainsRecord<string>(key).ConfigureAwait(false);
            Assert.IsTrue(result);

            await redis.Multiplexer.DeleteKeys("*Key*").ConfigureAwait(false);
            var value = await client.GetRecords<string>(key).LastOrDefaultAsync();
            Assert.IsNull(value);
            result = await redis.Client.ContainsRecord<string>(key).ConfigureAwait(false);
            Assert.IsFalse(result);
        }

        [Test]
        public async Task DeleteUsingClient()
        {
            var result = await redis.Client.ContainsRecord<string>(key).ConfigureAwait(false);
            Assert.IsFalse(result);
            var client = redis.Client;

            await client.AddRecord(key, "Test").ConfigureAwait(false);
            result = await redis.Client.ContainsRecord<string>(key).ConfigureAwait(false);
            Assert.IsTrue(result);

            await client.DeleteAll<string>(key).ConfigureAwait(false);
            var value = await client.GetRecords<string>(key).LastOrDefaultAsync();
            Assert.IsNull(value);

            result = await redis.Client.ContainsRecord<string>(key).ConfigureAwait(false);
            Assert.IsFalse(result);
        }

        [Test]
        public async Task ExpireKey()
        {
            var result = await redis.Client.ContainsRecord<string>(key).ConfigureAwait(false);
            Assert.IsFalse(result);
            var client = redis.Client;

            await client.AddRecord(key, "Test").ConfigureAwait(false);
            result = await redis.Client.ContainsRecord<string>(key).ConfigureAwait(false);
            Assert.IsTrue(result);

            await client.SetExpire<string>(key, TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            var value = await client.GetRecords<string>(key).LastOrDefaultAsync();
            Assert.IsNull(value);

            result = await redis.Client.ContainsRecord<string>(key).ConfigureAwait(false);
            Assert.IsFalse(result);
        }

        [Test]
        public async Task Flush()
        {
            var client = redis.Client;
            await client.AddRecord(key, "Test").ConfigureAwait(false);
            var value = await client.GetRecords<string>(key).LastOrDefaultAsync();
            Assert.AreEqual("Test", value);
            redis.Multiplexer.Flush();
            value = await client.GetRecords<string>(key).LastOrDefaultAsync();
            Assert.IsNull(value);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task KeyValue(bool isSet)
        {
            redis.RegisterHashType<Identity>();
            key.AddIndex(new IndexKey("Data", isSet));
            await redis.Client.AddRecord(key, routing).ConfigureAwait(false);
            var result = await redis.Client.GetRecords<Identity>(key).FirstAsync();
            Assert.AreEqual("Test", result.ApplicationId);
            Assert.AreEqual("DEV", result.Environment);
        }


        [Test]
        public async Task SaveComplex()
        {
            redis.RegisterHashType<ComplexData>();
            ComplexData data = new ComplexData();
            data.Date = new DateTime(2012, 02, 02);
            var newKey = new ObjectKey("Complex");
            await redis.Client.AddRecord(newKey, data).ConfigureAwait(false);
            var result = await redis.Client.GetRecords<ComplexData>(newKey).FirstAsync();
            Assert.AreEqual(data.Date, result.Date);
        }

        [Test]
        public async Task Single()
        {
            redis.RegisterHashType<Identity>().IsSingleInstance = true;
            routing.ApplicationId = null;
            await redis.Client.AddRecord(repositoryKey, routing).ConfigureAwait(false);
            repositoryKey.AddIndex(new IndexKey(repository.Object, "Data", true));
            await redis.Client.AddRecord(repositoryKey, routing).ConfigureAwait(false);
            var result = await redis.Client.GetRecords<Identity>(repositoryKey).ToArray();
            var result2 = await redis.Client.GetRecords<Identity>(new IndexKey(repository.Object, "Data", true)).ToArray();

            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(1, result2.Length);
            Assert.IsTrue(string.IsNullOrEmpty(result[0].ApplicationId));
            Assert.AreEqual("DEV", result[0].Environment);
        }

        [Test]
        public async Task Transaction()
        {
            var transaction = redis.StartTransaction();
            var task1 = transaction.Client.AddRecord(key, "Test");
            var rawResult = await redis.Client.GetRecords<string>(key).LastOrDefaultAsync();
            Assert.IsNull(rawResult);
            await transaction.Commit().ConfigureAwait(false);
            await task1.ConfigureAwait(false);
            rawResult = await redis.Client.GetRecords<string>(key).LastOrDefaultAsync();
            Assert.AreEqual("Test", rawResult);
        }

        [Test]
        public async Task VerifyGenericSerialization()
        {
            var order = new MainDataOne();
            order.Name = "G_Test";
            await redis.Client.AddRecord<IMainData>(new ObjectKey("order"), order).ConfigureAwait(false);
            var result = (MainDataOne)await redis.Client.GetRecords<IMainData>(new ObjectKey("order")).LastOrDefaultAsync();
            Assert.AreEqual(order.Name, result.Name);
        }

        [Test]
        public async Task Wellknown()
        {
            redis.RegisterWellknown<Identity>();
            await redis.Client.AddRecord(key, routing).ConfigureAwait(false);
            var result = await redis.Client.GetRecords<Identity>(key).FirstAsync();
            Assert.AreEqual("Test", result.ApplicationId);
            Assert.AreEqual("DEV", result.Environment);

            await redis.Client.DeleteAll<Identity>(key).ConfigureAwait(false);
            var total = await redis.Client.GetRecords<Identity>(key).ToArray();
            Assert.AreEqual(0, total.Length);
        }
    }
}