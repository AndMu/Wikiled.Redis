using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework.Legacy;
using Wikiled.Redis.Channels;
using Wikiled.Redis.IntegrationTests.MockData;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Serialization;

namespace Wikiled.Redis.IntegrationTests.Persistency
{
    [TestFixture]
    public class HashTests : BaseIntegrationTests
    {
        [Test]
        public async Task KeyValue()
        {
            Redis.PersistencyRegistration.RegisterHashSet<Identity>();
            Key.AddIndex(new IndexKey("Data"));
            await Redis.Client.AddRecord(Key, Routing);
            var result = await Redis.Client.GetRecords<Identity>(Key).FirstAsync();
            ClassicAssert.AreEqual("Test", result.ApplicationId);
            ClassicAssert.AreEqual("DEV", result.Environment);
        }

        [Test]
        public async Task SaveComplex()
        {
            Redis.PersistencyRegistration.RegisterHashSet<ComplexData>();
            var data = new ComplexData();
            data.Date = new DateTime(2012, 02, 02);
            var newKey = new ObjectKey("Complex");
            await Redis.Client.AddRecord(newKey, data);
            var result = await Redis.Client.GetRecords<ComplexData>(newKey).FirstAsync();
            ClassicAssert.AreEqual(data.Date, result.Date);
        }

        [Test]
        public async Task Single()
        {
            Redis.PersistencyRegistration.RegisterHashsetSingle<Identity>();
            Routing.ApplicationId = null;
            await Redis.Client.AddRecord(RepositoryKey, Routing);
            RepositoryKey.AddIndex(new IndexKey(Repository.Object, "Data"));
            await Redis.Client.AddRecord(RepositoryKey, Routing);
            var result = await Redis.Client.GetRecords<Identity>(RepositoryKey).ToArray();
            var result2 = await Redis.Client.GetRecords<Identity>(new IndexKey(Repository.Object, "Data")).ToArray();

            await Redis.Client.DeleteAll<Identity>(RepositoryKey);
            ClassicAssert.AreEqual(1, result.Length);
            ClassicAssert.AreEqual(1, result2.Length);
            ClassicAssert.IsTrue(string.IsNullOrEmpty(result[0].ApplicationId));
            ClassicAssert.AreEqual("DEV", result[0].Environment);
        }

        [Test]
        public async Task AddUpdate()
        {
            Redis.PersistencyRegistration.RegisterHashsetSingle<Identity>();
            RepositoryKey.AddIndex(new IndexKey(Repository.Object, "Data"));
            await Redis.Client.DeleteAll<Identity>(RepositoryKey);
            await Redis.Client.AddRecord(RepositoryKey, Routing);
            await Redis.Client.DeleteAll<Identity>(RepositoryKey);
            await Redis.Client.AddRecord(RepositoryKey, Routing);
        }

        [Test]
        public async Task TestIdentity()
        {
            Redis.PersistencyRegistration.RegisterHashsetSingle<Identity>();

            var key1 = new RepositoryKey(Repository.Object, new ObjectKey("Test1"));
            key1.AddIndex(ListAll);

            var items = Redis.Client.GetRecords<Identity>(ListAll).ToEnumerable().ToArray();
            ClassicAssert.AreEqual(0, items.Length);

            var identity = new Identity();
            identity.Environment = "Ev";
            await Redis.Client.AddRecord(key1, identity);

            var key2 = new RepositoryKey(Repository.Object, new ObjectKey("Test2"));
            key2.AddIndex(ListAll);
            await Redis.Client.AddRecord(key2, identity);

            items = Redis.Client.GetRecords<Identity>(ListAll).ToEnumerable().ToArray();
            ClassicAssert.AreEqual(2, items.Length);
            ClassicAssert.AreEqual("Ev", items[0].Environment);

            var count = await Redis.Client.Count(ListAll);
            ClassicAssert.AreEqual(2, count);
        }

        [Test]
        public async Task SaveDictionary()
        {
            Redis.PersistencyRegistration.RegisterHashSet(new DictionarySerializer(new[] { "Result" }));
            var table = new Dictionary<string, string>();
            table["Result"] = "one";
            await Redis.Client.AddRecord(Key, table);
            var records = await Redis.Client.GetRecords<Dictionary<string, string>>(Key).ToArray();
            ClassicAssert.AreEqual(1, records.Length);
            ClassicAssert.AreEqual("one", records[0]["Result"]);
        }
    }
}
