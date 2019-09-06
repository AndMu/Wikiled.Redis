using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Wikiled.Common.Logging;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Indexing;

namespace Wikiled.Redis.Logic
{
    public class RedisTransaction : IRedisTransaction
    {
        private readonly IRedisLink link;

        private static readonly ILogger log = ApplicationLogging.CreateLogger<RedisTransaction>();

        private readonly ITransaction transaction;

        public RedisTransaction(IRedisLink link, ITransaction transaction, IMainIndexManager indexManager)
        {
            this.transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            this.link = link ?? throw new ArgumentNullException(nameof(link));
            Client = new RedisClient(link, indexManager, transaction);
        }

        public IRedisClient Client { get; }

        public Task Commit()
        {
            if(link.State != ChannelState.Open)
            {
                log.LogWarning("Can't commit transaction with non open link");
                return Task.CompletedTask;
            }

            log.LogDebug("Commit");
            return transaction.ExecuteAsync();
        }
    }
}
