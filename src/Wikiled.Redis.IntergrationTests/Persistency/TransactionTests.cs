﻿using NUnit.Framework;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Wikiled.Redis.IntegrationTests.Persistency
{
    [TestFixture]
    public class TransactionTests : BaseIntegrationTests
    {
        [Test]
        public async Task AddToLimitedListTransaction()
        {
            var transaction = Redis.StartTransaction();
            var result = await Redis.Client.ContainsRecord<string>(RepositoryKey).ConfigureAwait(false);
            Assert.IsFalse(result);
            var task1 = transaction.Client.AddRecord(RepositoryKey, "Test1");
            var task2 = transaction.Client.AddRecord(RepositoryKey, "Test2");
            var task3 = transaction.Client.AddRecord(RepositoryKey, "Test3");
            await transaction.Commit().ConfigureAwait(false);
            await Task.WhenAll(task1, task2, task3).ConfigureAwait(false);
            result = await Redis.Client.ContainsRecord<string>(RepositoryKey).ConfigureAwait(false);
            Assert.IsTrue(result);
            var value = await Redis.Client.GetRecords<string>(RepositoryKey).ToArray();
            Assert.AreEqual(2, value.Length);
            Assert.AreEqual("Test2", value[0]);
            Assert.AreEqual("Test3", value[1]);
        }

        [Test]
        public async Task Transaction()
        {
            var transaction = Redis.StartTransaction();
            var task1 = transaction.Client.AddRecord(Key, "Test");
            var rawResult = await Redis.Client.GetRecords<string>(Key).LastOrDefaultAsync();
            Assert.IsNull(rawResult);
            await transaction.Commit().ConfigureAwait(false);
            await task1.ConfigureAwait(false);
            rawResult = await Redis.Client.GetRecords<string>(Key).LastOrDefaultAsync();
            Assert.AreEqual("Test", rawResult);
        }
    }
}
