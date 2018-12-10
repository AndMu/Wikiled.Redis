using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Wikiled.Common.Logging;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Serialization
{
    public class BaseSetSerialization
    {
        private static readonly ILogger log = ApplicationLogging.CreateLogger<BaseSetSerialization>();

        private readonly IRedisLink link;

        public BaseSetSerialization(IRedisLink link)
        {
            this.link = link ?? throw new ArgumentNullException(nameof(link));
        }

        public Task DeleteAll(IDatabaseAsync database, IDataKey key)
        {
            log.LogDebug("DeleteAll: [{0}]", key);
            return link.DeleteAll(database, key);
        }

        public Task SetExpire(IDatabaseAsync database, IDataKey key, TimeSpan timeSpan)
        {
            log.LogDebug("SetExpire: [{0}] - {1}", key, timeSpan);
            return link.SetExpire(database, key, timeSpan);
        }

        public Task SetExpire(IDatabaseAsync database, IDataKey key, DateTime dateTime)
        {
            log.LogDebug("SetExpire: [{0}] - {1}", key, dateTime);
            return link.SetExpire(database, key, dateTime);
        }

    }
}
