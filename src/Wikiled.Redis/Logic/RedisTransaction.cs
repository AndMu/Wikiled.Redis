using System;
using System.Threading.Tasks;
using NLog;
using StackExchange.Redis;
using Wikiled.Redis.Channels;

namespace Wikiled.Redis.Logic
{
    public class RedisTransaction : IRedisTransaction
    {
        private readonly IRedisLink link;

        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly ITransaction transaction;

        public RedisTransaction(IRedisLink link, ITransaction transaction)
        {
            this.transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            this.link = link ?? throw new ArgumentNullException(nameof(link));
            Client = new RedisClient(link, transaction);
        }

        public IRedisClient Client { get; }

        public Task Commit()
        {
            if(link.State != ChannelState.Open)
            {
                log.Warn("Can't commit transaction with non open link");
                return Task.CompletedTask;
            }

            log.Debug("Commit");
            return transaction.ExecuteAsync();
        }
    }
}
