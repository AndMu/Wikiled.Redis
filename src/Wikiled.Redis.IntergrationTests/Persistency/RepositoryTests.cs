using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.IO;
using Wikiled.Common.Utilities.Serialization;
using Wikiled.Redis.Channels;
using Wikiled.Redis.IntegrationTests.Helpers;
using Wikiled.Redis.IntegrationTests.MockData;

namespace Wikiled.Redis.IntegrationTests.Persistency
{
    [TestFixture]
    public class RepositoryTests : BaseIntegrationTests
    {
        [Test]
        public async Task TestRepository()
        {
            var repository = new IdentityRepository(new NullLogger<IdentityRepository>(), Redis);

            var tasks = new List<Task>();
            for (int i = 0; i < 2; i++)
            {
                tasks.Add(repository.Save(new Identity { InstanceId = i.ToString() }, repository.Entity.AllIndex));
            }

            await Task.WhenAll(tasks);
        }

        [Test]
        public async Task TestRepositoryEx()
        {
            var repository = new IdentityRepositoryEx(new NullLogger<IdentityRepository>(), Redis, new BasicJsonSerializer(new RecyclableMemoryStreamManager()));

            var result = await repository.LoadAll().ToArray();
            Assert.AreEqual(0, result.Length);

            var total = await repository.Count();
            Assert.AreEqual(0, total);
            
            for (int i = 0; i < 2; i++)
            {
                await repository.Save(new Identity { InstanceId = i.ToString() }, repository.Entity.AllIndex);
            }

            total = await repository.Count();
            Assert.AreEqual(2, total);
            result = await repository.LoadAll().ToArray();
            Assert.AreEqual(2, result.Length);
        }

        [Test]
        public async Task TestEvents()
        {
            var repository = new IdentityRepository(new NullLogger<IdentityRepository>(), Redis);
            repository.SubscribeToChanges().Subscribe(item => { });
            var result = repository.SubscribeToChanges().Take(2).Timeout(TimeSpan.FromSeconds(5)).ToArray().GetAwaiter();

            await Task.Delay(100);
            var tasks = new List<Task>();
            for (int i = 0; i < 2; i++)
            {
                tasks.Add(repository.Save(new Identity { InstanceId = i.ToString() }, repository.Entity.AllIndex));
            }

            await Task.WhenAll(tasks);
            var received = await result;
            
            Assert.AreEqual(2, received.Length);
        }

        [Test]
        public async Task TestNestedRepository()
        {
            var repositoryInner = new IdentityRepository(new NullLogger<IdentityRepository>(), Redis);
            var repository = new SimpleItemRepository(new NullLogger<SimpleItemRepository>(), Redis, repositoryInner);

            var transaction = Redis.StartTransaction();
            var tasks = new List<Task>();
            for (int i = 0; i < 2; i++)
            {
                tasks.Add(repository.Save(new SimpleItem { Id = i }, transaction));
            }

            await transaction.Commit();

            await Task.WhenAll(tasks);

            var all = await repository.LoadAll(repository.Entity.AllIndex).ToArray();
            Assert.AreEqual(2, all.Length);

            tasks.Clear();
            transaction = Redis.StartTransaction();
            for (int i = 0; i < 2; i++)
            {
                tasks.Add(repository.Delete(i.ToString()));
            }

            await transaction.Commit();
            await Task.WhenAll(tasks);
            all = await repository.LoadAll(repository.Entity.AllIndex).ToArray();
            Assert.AreEqual(0, all.Length);
        }

        [Test]
        public async Task TestNestedRepositorySequential()
        {
            var repositoryInner = new IdentityRepository(new NullLogger<IdentityRepository>(), Redis);
            var repository = new SimpleItemRepository(new NullLogger<SimpleItemRepository>(), Redis, repositoryInner);

            for (int i = 0; i < 10; i++)
            {
                await Resilience.AsyncRetryPolicy
                                .ExecuteAsync(() => repository.Save(new SimpleItem { Id = i }, repository.Entity.AllIndex))
                                ;
            }
        }


        [Test]
        public async Task Activation()
        {
            var repository = new IdentityRepository(new NullLogger<IdentityRepository>(), Redis);

            var tasks = new List<Task>();
            for (int i = 0; i < 2; i++)
            {
                tasks.Add(repository.Save(new Identity { InstanceId = i.ToString() }, repository.Entity.AllIndex));
            }

            await Task.WhenAll(tasks);
            
            var loaded = await repository.LoadAll().ToArray();
            Assert.AreEqual(2, loaded.Length);
            
            loaded = await repository.LoadActive().ToArray();
            Assert.AreEqual(2, loaded.Length);

            loaded = await repository.LoadInActive().ToArray();
            Assert.AreEqual(0, loaded.Length);


            await repository.Deactivate("1");

            loaded = await repository.LoadAll().ToArray();
            Assert.AreEqual(2, loaded.Length);

            loaded = await repository.LoadActive().ToArray();
            Assert.AreEqual(1, loaded.Length);

            loaded = await repository.LoadInActive().ToArray();
            Assert.AreEqual(1, loaded.Length);
        }
    }
}
