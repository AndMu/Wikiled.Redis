using NUnit.Framework;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework.Legacy;

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
            var result = await Redis.Client.ContainsRecord<string>(RepositoryKey);
            ClassicAssert.IsFalse(result);
            var task1 = transaction.Client.AddRecord(RepositoryKey, "Test1");
            var task2 = transaction.Client.AddRecord(RepositoryKey, "Test2");
            var task3 = transaction.Client.AddRecord(RepositoryKey, "Test3");
            await transaction.Commit();
            await Task.WhenAll(task1, task2, task3);
            result = await Redis.Client.ContainsRecord<string>(RepositoryKey);
            ClassicAssert.IsTrue(result);
            var value = await Redis.Client.GetRecords<string>(RepositoryKey).ToArray();
            ClassicAssert.AreEqual(2, value.Length);
            ClassicAssert.AreEqual("Test2", value[0]);
            ClassicAssert.AreEqual("Test3", value[1]);
        }

        [Test]
        public async Task Transaction()
        {
            Key.AddIndex(ListAll);
            var transaction = Redis.StartTransaction();
            var task1 = transaction.Client.AddRecord(Key, "Test");
            var rawResult = await Redis.Client.GetRecords<string>(Key).LastOrDefaultAsync();
            ClassicAssert.IsNull(rawResult);
            await transaction.Commit();
            await task1;
            rawResult = await Redis.Client.GetRecords<string>(Key).LastOrDefaultAsync();
            ClassicAssert.AreEqual("Test", rawResult);
        }
    }
}
