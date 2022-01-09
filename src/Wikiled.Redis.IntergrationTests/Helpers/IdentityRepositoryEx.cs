using Microsoft.Extensions.Logging;
using Wikiled.Common.Utilities.Serialization;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Data;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Persistency;

namespace Wikiled.Redis.IntegrationTests.Helpers
{
    public class IdentityRepositoryEx : TrackingEntityRepository<Identity>
    {
        public IdentityRepositoryEx(ILogger<IdentityRepository> log, IRedisLink redis, IJsonSerializer serializer)
            : base(log, redis, "Identity", register: handler => handler.RegisterObjectHashSingle<Identity>(new JsonDataSerializer(serializer)))
        {
        }

        protected override string GetRecordId(Identity instance)
        {
            return instance.InstanceId;
        }
    }
}
