using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wikiled.Redis.Channels;
using Wikiled.Redis.IntegrationTests.MockData;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Persistency;

namespace Wikiled.Redis.IntegrationTests.Helpers
{
    public class SimpleItemRepository : EntityRepository<SimpleItem>
    {
        private readonly IdentityRepository inner;

        public SimpleItemRepository(ILogger<SimpleItemRepository> log, IRedisLink redis, IdentityRepository inner)
            : base(log, redis, "SimpleItem")
        {
            this.inner = inner;
        }

        protected override string GetRecordId(SimpleItem instance)
        {
            return instance.Id.ToString();
        }

        protected override async Task BeforeSaving(IRedisTransaction transaction, IDataKey key, SimpleItem entity)
        {
            var identity = new Identity();
            identity.InstanceId = "One";
            await inner.Save(identity, transaction);
        }
    }
}
