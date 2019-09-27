using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Persistency;

namespace Wikiled.Redis.IntegrationTests.Helpers
{
    public class IdentityRepository : EntityRepository<Identity>
    {
        public IdentityRepository(ILogger<EntityRepository<Identity>> log, IRedisLink redis)
            : base(log, redis, "Identity")
        {
        }

        protected override string GetRecordId(Identity instance)
        {
            return instance.InstanceId;
        }

        protected override Task BeforeSaving(IRedisTransaction transaction, IDataKey key)
        {
            return Task.CompletedTask;
        }
    }
}
