using Microsoft.Extensions.Logging;
using Wikiled.Common.Utilities.Serialization;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Data;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Persistency;

namespace Wikiled.Redis.IntegrationTests.Helpers
{
    public class IdentityListRepository : EntityRepository<Identity>
    {
        public IdentityListRepository(ILogger<EntityRepository<Identity>> log, IRedisLink redis, IJsonSerializer serializer)
            : base(log, redis, "MarketData", register: handler => handler.RegisterList<Identity>(new JsonDataSerializer(serializer)))
        {
        }

        protected override string GetRecordId(Identity instance)
        {
            return instance.InstanceId;
        }
    }
}
