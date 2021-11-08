using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Microsoft.IO;
using Wikiled.Common.Utilities.Serialization;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Data;
using Wikiled.Redis.Helpers;
using Wikiled.Redis.Indexing;
using Wikiled.Redis.IntegrationTests.MockData;
using Wikiled.Redis.Keys;

namespace Wikiled.Redis.IntegrationTests.Persistency
{
    [TestFixture]
    public class RedisPersistencyTests : BaseIntegrationTests
    {
        private RecyclableMemoryStreamManager stream = new RecyclableMemoryStreamManager();

        [Test]
        public async Task AddToLimitedList()
        {
            var result = await Redis.Client.ContainsRecord<string>(RepositoryKey).ConfigureAwait(false);
            Assert.IsFalse(result);
            await Redis.Client.AddRecord(RepositoryKey, "Test1").ConfigureAwait(false);
            result = await Redis.Client.ContainsRecord<string>(RepositoryKey).ConfigureAwait(false);
            Assert.IsTrue(result);
            await Redis.Client.AddRecord(RepositoryKey, "Test2").ConfigureAwait(false);
            await Redis.Client.AddRecord(RepositoryKey, "Test3").ConfigureAwait(false);
            var value = await Redis.Client.GetRecords<string>(RepositoryKey).ToArray();
            var count = await Redis.Client.Count<string>(RepositoryKey).ConfigureAwait(false);
            Assert.AreEqual(2, count);
            Assert.AreEqual(2, value.Length);
            Assert.AreEqual(2, value.Length);
            Assert.AreEqual("Test2", value[0]);
            Assert.AreEqual("Test3", value[1]);
        }

        [TestCase(1000)]
        [TestCase(2)]
        public async Task GetHash(int total)
        {
            for (var i = 0; i < total; i++)
            {
                Redis.Database.HashSet("Test", i, i);
            }

            var result = await Redis.GetHash("Test").ToArray();
            Assert.AreEqual(total, result.Length);
        }

        [Test]
        public async Task Primitive()
        {
            await Redis.Client.AddRecord(RepositoryKey, 1).ConfigureAwait(false);
            var result = await Redis.Client.GetRecords<int>(RepositoryKey).ToArray().ToTask().ConfigureAwait(false);
            Assert.AreEqual(1, result[0]);

            await Redis.Client.AddRecord(RepositoryKey, "1").ConfigureAwait(false);
            var resultString = await Redis.Client.GetRecords<string>(RepositoryKey).ToArray().ToTask().ConfigureAwait(false);
            Assert.AreEqual("1", resultString[0]);
        }

        [TestCase(1, 3)]
        [TestCase(2, 2)]
        [TestCase(3, 1)]
        [TestCase(4, 3)]
        [TestCase(5, 3)]
        [TestCase(6, 1)]
        [TestCase(7, 3)]
        public async Task TestMultiple(int type, int total)
        {
            SertupPersistency(type);
            var key1 = new RepositoryKey(Repository.Object, new ObjectKey("Test1"));
            await Redis.Client.AddRecord(key1, new Identity { Environment = "Test1" }).ConfigureAwait(false);
            await Redis.Client.AddRecord(key1, new Identity { Environment = "Test2" }).ConfigureAwait(false);
            await Redis.Client.AddRecord(key1, new Identity { Environment = "Test3" }).ConfigureAwait(false);

            var items = Redis.Client.GetRecords<Identity>(key1).ToEnumerable().OrderBy(item => item.Environment).ToArray();
            Assert.AreEqual(total, items.Length);
        }

        [TestCase(1, 10)]
        [TestCase(1, 1)]
        [TestCase(2, 10)]
        [TestCase(2, 1)]
        [TestCase(3, 10)]
        [TestCase(3, 1)]
        [TestCase(4, 10)]
        [TestCase(4, 1)]
        [TestCase(5, 10)]
        [TestCase(5, 1)]
        [TestCase(6, 10)]
        [TestCase(6, 1)]
        [TestCase(7, 1)]
        [TestCase(7, 10)]
        public async Task TestIndex(int type, int batchSize)
        {
            Redis.Client.BatchSize = batchSize;
            SertupPersistency(type);
            
            var key1 = new RepositoryKey(Repository.Object, new ObjectKey("Test1"));
            key1.AddIndex(ListAll);
            key1.AddIndex(ListAll2);
            await Redis.Client.AddRecord(key1, new Identity { Environment = "Test1" }).ConfigureAwait(false);

            var key2 = new RepositoryKey(Repository.Object, new ObjectKey("Test2"));
            key2.AddIndex(ListAll);
            key2.AddIndex(ListAll2);
            await Redis.Client.AddRecord(key2, new Identity { Environment = "Test2" }).ConfigureAwait(false);

            var key3 = new RepositoryKey(Repository.Object, new ObjectKey("Test3"));
            key3.AddIndex(ListAll);
            await Redis.Client.AddRecord(key3, new Identity { Environment = "Test3" }).ConfigureAwait(false);

            var items = Redis.Client.GetRecords<Identity>(ListAll).ToEnumerable().OrderBy(item => item.Environment).ToArray();
            Assert.AreEqual(3, items.Length);
            Assert.AreEqual("Test1", items[0].Environment);
            Assert.AreEqual("Test2", items[1].Environment);
            Assert.AreEqual("Test3", items[2].Environment);

            items = Redis.Client.GetRecords<Identity>(ListAll2).ToEnumerable().OrderBy(item => item.Environment).ToArray();
            Assert.AreEqual(2, items.Length);
            Assert.AreEqual("Test1", items[0].Environment);
            Assert.AreEqual("Test2", items[1].Environment);

            items = await Redis.Client.GetRecords<Identity>(ListAll, 0, 0).ToArray();
            Assert.AreEqual(1, items.Length);
            Assert.AreEqual("Test3", items[0].Environment);

            items = Redis.Client.GetRecords<Identity>(ListAll, 1, 3).ToEnumerable().OrderBy(item => item.Environment).ToArray();
            Assert.AreEqual(2, items.Length);
            Assert.AreEqual("Test1", items[0].Environment);
            Assert.AreEqual("Test2", items[1].Environment);

            items = await Redis.Client.GetRecords<Identity>(ListAll, 3, 4).ToArray();
            Assert.AreEqual(0, items.Length);

            var factory = new IndexManagerFactory(new NullLoggerFactory(), Redis);
            var manager = factory.Create(ListAll);
            var count = await manager.Count(Redis.Database, ListAll).ConfigureAwait(false);
            Assert.AreEqual(3, count);

            count = await Redis.Client.Count(ListAll).ConfigureAwait(false);
            Assert.AreEqual(3, count);

            count = (await manager.GetIds(Redis.Database, ListAll).ToArray()).Length;
            Assert.AreEqual(3, count);

            var result = await manager.GetIds(Redis.Database, ListAll, 1, 2).ToArray();
            Assert.AreEqual("Test2", (string)result[0]);
            result = await manager.GetIds(Redis.Database, ListAll, 0, 1).ToArray();
            Assert.AreEqual("Test3", (string)result[0]);

            await manager.Reset(Redis.Database, ListAll).ConfigureAwait(false);
            count = await manager.Count(Redis.Database, ListAll).ConfigureAwait(false);
            Assert.AreEqual(0, count);

            await Redis.Reindex(new NullLoggerFactory(), key2).ConfigureAwait(false);
            count = await manager.Count(Redis.Database, ListAll).ConfigureAwait(false);
            Assert.AreEqual(3, count);
        }
      
        [Test]
        public async Task SaveSortedList()
        {
            await Redis.Client.AddRecord(RepositoryKey, new SortedSetEntry("One", 1))
                       .ConfigureAwait(false);
            await Redis.Client.AddRecord(RepositoryKey, new SortedSetEntry("Two", 2))
                       .ConfigureAwait(false);
            await Redis.Client.AddRecord(RepositoryKey, new SortedSetEntry("Fine", -2))
                       .ConfigureAwait(false);
            var result = await Redis.Client.GetRecords<SortedSetEntry>(RepositoryKey).ToArray();
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual("Fine", (string)result[0].Element);
            Assert.AreEqual("One", (string)result[1].Element);
            Assert.AreEqual("Two", (string)result[2].Element);
            
            var count = await Redis.Client.Count<SortedSetEntry>(RepositoryKey).ConfigureAwait(false);
            Assert.AreEqual(3, count);
        }
       
        [Test]
        public async Task DeleteKeys()
        {
            var result = await Redis.Client.ContainsRecord<string>(Key).ConfigureAwait(false);
            Assert.IsFalse(result);
            var client = Redis.Client;

            await client.AddRecord(Key, "Test").ConfigureAwait(false);
            result = await Redis.Client.ContainsRecord<string>(Key).ConfigureAwait(false);
            Assert.IsTrue(result);

            await Redis.Multiplexer.DeleteKeys("*Key*").ConfigureAwait(false);
            var value = await client.GetRecords<string>(Key).LastOrDefaultAsync();
            Assert.IsNull(value);
            result = await Redis.Client.ContainsRecord<string>(Key).ConfigureAwait(false);
            Assert.IsFalse(result);
        }

        [Test]
        public async Task DeleteUsingClient()
        {
            var result = await Redis.Client.ContainsRecord<string>(Key).ConfigureAwait(false);
            Assert.IsFalse(result);
            var client = Redis.Client;

            await client.AddRecord(Key, "Test").ConfigureAwait(false);
            result = await Redis.Client.ContainsRecord<string>(Key).ConfigureAwait(false);
            Assert.IsTrue(result);

            await client.DeleteAll<string>(Key).ConfigureAwait(false);
            var value = await client.GetRecords<string>(Key).LastOrDefaultAsync();
            Assert.IsNull(value);

            result = await Redis.Client.ContainsRecord<string>(Key).ConfigureAwait(false);
            Assert.IsFalse(result);
        }

        [Test]
        public async Task ExpireKey()
        {
            var result = await Redis.Client.ContainsRecord<string>(Key).ConfigureAwait(false);
            Assert.IsFalse(result);
            var client = Redis.Client;

            await client.AddRecord(Key, "Test").ConfigureAwait(false);
            result = await Redis.Client.ContainsRecord<string>(Key).ConfigureAwait(false);
            Assert.IsTrue(result);

            await client.SetExpire<string>(Key, TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            var value = await client.GetRecords<string>(Key).LastOrDefaultAsync();
            Assert.IsNull(value);

            result = await Redis.Client.ContainsRecord<string>(Key).ConfigureAwait(false);
            Assert.IsFalse(result);
        }

        [Test]
        public async Task Flush()
        {
            var client = Redis.Client;
            await client.AddRecord(Key, "Test").ConfigureAwait(false);
            var value = await client.GetRecords<string>(Key).LastOrDefaultAsync();
            Assert.AreEqual("Test", value);
            Redis.Multiplexer.Flush();
            value = await client.GetRecords<string>(Key).LastOrDefaultAsync();
            Assert.IsNull(value);
        }

      
        [Test]
        public async Task VerifyGenericSerialization()
        {
            var order = new MainDataOne();
            order.Name = "G_Test";
            await Redis.Client.AddRecord<IMainData>(new ObjectKey("order"), order).ConfigureAwait(false);
            var result = (MainDataOne)await Redis.Client.GetRecords<IMainData>(new ObjectKey("order")).LastOrDefaultAsync();
            Assert.AreEqual(order.Name, result.Name);
        }

        [Test]
        public async Task Wellknown()
        {
            Redis.PersistencyRegistration.RegisterObjectHashList<Identity>(new XmlDataSerializer(), true);
            await Redis.Client.AddRecord(Key, Routing).ConfigureAwait(false);
            var result = await Redis.Client.GetRecords<Identity>(Key).FirstAsync();
            Assert.AreEqual("Test", result.ApplicationId);
            Assert.AreEqual("DEV", result.Environment);

            await Redis.Client.DeleteAll<Identity>(Key).ConfigureAwait(false);
            var total = await Redis.Client.GetRecords<Identity>(Key).ToArray();
            Assert.AreEqual(0, total.Length);
        }

        private void SertupPersistency(int type)
        {
            switch (type)
            {
                case 1:
                    Redis.PersistencyRegistration.RegisterSet<Identity>(new XmlDataSerializer());
                    break;
                case 2:
                    Redis.PersistencyRegistration.RegisterList<Identity>(new XmlDataSerializer());
                    break;
                case 3:
                    Redis.PersistencyRegistration.RegisterHashsetSingle<Identity>();
                    break;
                case 4:
                    Redis.PersistencyRegistration.RegisterHashsetList<Identity>();
                    break;
                case 5:
                    Redis.PersistencyRegistration.RegisterObjectHashList<Identity>(new XmlDataSerializer());
                    break;
                case 6:
                    Redis.PersistencyRegistration.RegisterObjectHashSingle<Identity>(new XmlDataSerializer());
                    break;
                case 7:
                    Redis.PersistencyRegistration.RegisterSet<Identity>(new JsonDataSerializer(new BasicJsonSerializer(stream)));
                    break;
            }
        }
    }
}