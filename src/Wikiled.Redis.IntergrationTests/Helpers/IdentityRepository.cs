﻿using Microsoft.Extensions.Logging;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Persistency;

namespace Wikiled.Redis.IntegrationTests.Helpers
{
    public class IdentityRepository : TrackingEntityRepository<Identity>
    {
        public IdentityRepository(ILogger<IdentityRepository> log, IRedisLink redis)
            : base(log, redis, "Identity")
        {
        }

        protected override string GetRecordId(Identity instance)
        {
            return instance.InstanceId;
        }
    }
}
