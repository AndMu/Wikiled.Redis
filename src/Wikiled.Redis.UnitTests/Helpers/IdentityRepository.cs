using Microsoft.Extensions.Logging;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Persistency;

namespace Wikiled.Redis.UnitTests.Helpers
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
    }
}
