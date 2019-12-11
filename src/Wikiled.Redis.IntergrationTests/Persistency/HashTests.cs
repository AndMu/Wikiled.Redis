using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Wikiled.Redis.Channels;
using Wikiled.Redis.IntegrationTests.MockData;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Serialization;

namespace Wikiled.Redis.IntegrationTests.Persistency
{
    [TestFixture]
    public class HashTests : BaseIntegrationTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public async Task KeyValue(bool isSet)
        {
            Redis.PersistencyRegistration.RegisterHashsetList<Identity>();
            Key.AddIndex(new IndexKey("Data", isSet));
            await Redis.Client.AddRecord(Key, Routing).ConfigureAwait(false);
            var result = await Redis.Client.GetRecords<Identity>(Key).FirstAsync();
            Assert.AreEqual("Test", result.ApplicationId);
            Assert.AreEqual("DEV", result.Environment);
        }

        [Test]
        public async Task SaveComplex()
        {
            Redis.PersistencyRegistration.RegisterHashsetList<ComplexData>();
            var data = new ComplexData();
            data.Date = new DateTime(2012, 02, 02);
            var newKey = new ObjectKey("Complex");
            await Redis.Client.AddRecord(newKey, data).ConfigureAwait(false);
            var result = await Redis.Client.GetRecords<ComplexData>(newKey).FirstAsync();
            Assert.AreEqual(data.Date, result.Date);
        }

        [Test]
        public async Task Single()
        {
            Redis.PersistencyRegistration.RegisterHashsetSingle<Identity>();
            Routing.ApplicationId = null;
            await Redis.Client.AddRecord(RepositoryKey, Routing).ConfigureAwait(false);
            RepositoryKey.AddIndex(new IndexKey(Repository.Object, "Data", true));
            await Redis.Client.AddRecord(RepositoryKey, Routing).ConfigureAwait(false);
            var result = await Redis.Client.GetRecords<Identity>(RepositoryKey).ToArray();
            var result2 = await Redis.Client.GetRecords<Identity>(new IndexKey(Repository.Object, "Data", true)).ToArray();

            await Redis.Client.DeleteAll<Identity>(RepositoryKey).ConfigureAwait(false);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(1, result2.Length);
            Assert.IsTrue(string.IsNullOrEmpty(result[0].ApplicationId));
            Assert.AreEqual("DEV", result[0].Environment);
        }

        [Test]
        public async Task AddUpdate()
        {
            Redis.PersistencyRegistration.RegisterHashsetSingle<Identity>();
            RepositoryKey.AddIndex(new IndexKey(Repository.Object, "Data", true));
            await Redis.Client.DeleteAll<Identity>(RepositoryKey).ConfigureAwait(false);
            await Redis.Client.AddRecord(RepositoryKey, Routing).ConfigureAwait(false);
            await Redis.Client.DeleteAll<Identity>(RepositoryKey).ConfigureAwait(false);
            await Redis.Client.AddRecord(RepositoryKey, Routing).ConfigureAwait(false);
        }

        [Test]
        public async Task TestIdentity()
        {
            Redis.PersistencyRegistration.RegisterHashsetSingle<Identity>();

            var key1 = new RepositoryKey(Repository.Object, new ObjectKey("Test1"));
            key1.AddIndex(ListAll);

            var items = Redis.Client.GetRecords<Identity>(ListAll).ToEnumerable().ToArray();
            Assert.AreEqual(0, items.Length);

            var identity = new Identity();
            identity.Environment = "Ev";
            await Redis.Client.AddRecord(key1, identity).ConfigureAwait(false);

            var key2 = new RepositoryKey(Repository.Object, new ObjectKey("Test2"));
            key2.AddIndex(ListAll);
            await Redis.Client.AddRecord(key2, identity).ConfigureAwait(false);

            items = Redis.Client.GetRecords<Identity>(ListAll).ToEnumerable().ToArray();
            Assert.AreEqual(2, items.Length);
            Assert.AreEqual("Ev", items[0].Environment);

            var count = await Redis.Client.Count(ListAll).ConfigureAwait(false);
            Assert.AreEqual(2, count);
        }

        [Test]
        public async Task SaveDictionary()
        {
            Redis.PersistencyRegistration.RegisterHashsetList(new DictionarySerializer(new[] { "Result" }));
            var table = new Dictionary<string, string>();
            table["Result"] = "one";
            await Redis.Client.AddRecord(Key, table).ConfigureAwait(false);
            var records = await Redis.Client.GetRecords<Dictionary<string, string>>(Key).ToArray();
            Assert.AreEqual(1, records.Length);
            Assert.AreEqual("one", records[0]["Result"]);
        }
    }
}
