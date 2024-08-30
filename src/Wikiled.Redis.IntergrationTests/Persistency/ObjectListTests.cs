using NUnit.Framework;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework.Legacy;
using Wikiled.Common.Utilities.Helpers;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Data;
using Wikiled.Redis.IntegrationTests.MockData;
using Wikiled.Redis.Keys;

namespace Wikiled.Redis.IntegrationTests.Persistency
{
    [TestFixture]
    public class ObjectListTests : BaseIntegrationTests
    {
        private ComplexData data;

        public override Task Setup()
        {
            data = new ComplexData();
            data.Date = new DateTime(2012, 02, 02);
            return base.Setup();
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task SaveSingle(bool isWellKnown)
        {
            Redis.PersistencyRegistration.RegisterObjectHashSet<ComplexData>(new FlatProtoDataSerializer(isWellKnown, MemoryStreamInstances.MemoryStream), isWellKnown);
            var newKey = new ObjectKey("Complex");
            await Redis.Client.AddRecord(newKey, data);
            var result = await Redis.Client.GetRecords<ComplexData>(newKey).FirstAsync();
            ClassicAssert.AreEqual(data.Date, result.Date);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task SaveMultiple(bool isWellKnown)
        {
            Redis.PersistencyRegistration.RegisterObjectHashSet<ComplexData>(new FlatProtoDataSerializer(isWellKnown, MemoryStreamInstances.MemoryStream), isWellKnown);
            var newKey = new ObjectKey("Complex");

            for (int i = 0; i < 10; i++)
            {
                await Redis.Client.AddRecord(newKey, data);
            }
            
            var result = await Redis.Client.GetRecords<ComplexData>(newKey).ToArray();
            ClassicAssert.AreEqual(10, result.Length);
        }

        [Test]
        public async Task SaveKnowLoadUnknown()
        {
            Redis.PersistencyRegistration.RegisterObjectHashSet<ComplexData>(
                new FlatProtoDataSerializer(true, MemoryStreamInstances.MemoryStream),
                true);
            var newKey = new ObjectKey("Complex");
            await Redis.Client.AddRecord(newKey, data);

            Redis.PersistencyRegistration.RegisterObjectHashSet<Identity>(new FlatProtoDataSerializer(false, MemoryStreamInstances.MemoryStream));
            ClassicAssert.ThrowsAsync<ArgumentNullException>(async () => await Redis.Client.GetRecords<Identity>(newKey).FirstAsync());
        }
    }
}
