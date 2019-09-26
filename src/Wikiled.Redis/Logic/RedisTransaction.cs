using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Indexing;

namespace Wikiled.Redis.Logic
{
    public class RedisTransaction : IRedisTransaction
    {
        private readonly IRedisLink link;

        private readonly ILogger<RedisTransaction> log;

        private readonly ITransaction transaction;

        public RedisTransaction(ILoggerFactory loggerFactory, IRedisLink link, ITransaction transaction, IMainIndexManager indexManager)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this.transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            this.link = link ?? throw new ArgumentNullException(nameof(link));
            log = loggerFactory.CreateLogger<RedisTransaction>();
            Client = new RedisClient(loggerFactory.CreateLogger<RedisClient>(), link, indexManager, transaction);
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
