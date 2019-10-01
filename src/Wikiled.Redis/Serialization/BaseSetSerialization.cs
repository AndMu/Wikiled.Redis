using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Wikiled.Common.Logging;
using Wikiled.Redis.Indexing;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Serialization
{
    public abstract class BaseSetSerialization
    {
        private readonly ILogger log;

        private readonly IRedisLink link;

        private readonly IMainIndexManager mainIndexManager;

        protected BaseSetSerialization(ILogger log, IRedisLink link, IMainIndexManager mainIndexManager)
        {
            this.link = link ?? throw new ArgumentNullException(nameof(link));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.mainIndexManager = mainIndexManager ?? throw new ArgumentNullException(nameof(mainIndexManager));
        }

        public Task DeleteAll(IDatabaseAsync database, IDataKey key)
        {
            log.LogDebug("DeleteAll: [{0}]", key);
            var tasks = new List<Task>(mainIndexManager.Delete(database, key));
            tasks.Add(link.DeleteAll(database, key));
            return Task.WhenAll(tasks);
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
