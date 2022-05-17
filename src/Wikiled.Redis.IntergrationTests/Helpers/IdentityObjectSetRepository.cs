using Microsoft.Extensions.Logging;
using Wikiled.Common.Utilities.Serialization;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Data;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Persistency;

namespace Wikiled.Redis.IntegrationTests.Helpers
{
    public class IdentityObjectSetRepository : EntityRepository<Identity>
    {
        public IdentityObjectSetRepository(ILogger<EntityRepository<Identity>> log, IRedisLink redis, IJsonSerializer serializer)
            : base(log, redis, "MarketData", register: handler => handler.RegisterObjectHashSet<Identity>(new JsonDataSerializer(serializer)))
        {
        }

        protected override string GetRecordId(Identity instance)
        {
            return instance.InstanceId;
        }
    }
}
