using System;
using System.Threading.Tasks;
using NLog;
using StackExchange.Redis;
using Wikiled.Common.Arguments;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Serialization
{
    public class BaseSetSerialization
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly IRedisLink link;

        public BaseSetSerialization(IRedisLink link)
        {
            Guard.NotNull(() => link, link);
            this.link = link;
        }

        public Task DeleteAll(IDatabaseAsync database, IDataKey key)
        {
            log.Debug("DeleteAll: [{0}]", key);
            return link.DeleteAll(database, key);
        }

        public Task SetExpire(IDatabaseAsync database, IDataKey key, TimeSpan timeSpan)
        {
            log.Debug("SetExpire: [{0}] - {1}", key, timeSpan);
            return link.SetExpire(database, key, timeSpan);
        }

        public Task SetExpire(IDatabaseAsync database, IDataKey key, DateTime dateTime)
        {
            log.Debug("SetExpire: [{0}] - {1}", key, dateTime);
            return link.SetExpire(database, key, dateTime);
        }

    }
}
