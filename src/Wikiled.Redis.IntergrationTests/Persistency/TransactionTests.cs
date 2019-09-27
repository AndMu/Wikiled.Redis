﻿using System.Collections.Generic;
using NUnit.Framework;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Wikiled.Redis.Channels;
using Wikiled.Redis.IntegrationTests.Helpers;
using Wikiled.Redis.Persistency;

namespace Wikiled.Redis.IntegrationTests.Persistency
{
    [TestFixture]
    public class TransactionTests : BaseIntegrationTests
    {
        [Test]
        public async Task AddToLimitedListTransaction()
        {
            var transaction = Redis.StartTransaction();
            RepositoryKey.AddIndex(ListAll);
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
            Key.AddIndex(ListAll);
            var transaction = Redis.StartTransaction();
            var task1 = transaction.Client.AddRecord(Key, "Test");
            var rawResult = await Redis.Client.GetRecords<string>(Key).LastOrDefaultAsync();
            Assert.IsNull(rawResult);
            await transaction.Commit().ConfigureAwait(false);
            await task1.ConfigureAwait(false);
            rawResult = await Redis.Client.GetRecords<string>(Key).LastOrDefaultAsync();
            Assert.AreEqual("Test", rawResult);
        }

        [Test]
        public async Task Repository()
        {
            var repository = new IdentityRepository(new NullLogger<EntityRepository<Identity>>(), Redis);

            var tasks = new List<Task>();
            for (int i = 0; i < 2; i++)
            {
                tasks.Add(repository.Save(new Identity { InstanceId = i.ToString() }, repository.Entity.AllIndex));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
