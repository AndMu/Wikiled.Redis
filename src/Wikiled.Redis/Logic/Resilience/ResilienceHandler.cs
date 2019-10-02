using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using StackExchange.Redis;

namespace Wikiled.Redis.Logic.Resilience
{
    public class ResilienceHandler : IResilience
    {
        private readonly ILogger<ResilienceHandler> logger;

        private readonly ResilienceConfig config;

        public ResilienceHandler(ILogger<ResilienceHandler> logger, ResilienceConfig config)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.config = config ?? throw new ArgumentNullException(nameof(config));

            var policy = Policy
                         .Handle<RedisTimeoutException>()
                         .Or<RedisConnectionException>()
                         .Or<RedisException>()
                         .Or<RedisServerException>();

            RetryPolicy = policy
                          .WaitAndRetry(
                              5,
                              (retries, ex, ctx) => DelayRoutine(ex, retries),
                              (ts, i, ctx, task) => { });

            AsyncRetryPolicy = policy
                .WaitAndRetryAsync(
                    5,
                    (retries, ex, ctx) => DelayRoutine(ex, retries),
                    (ts, i, ctx, task) => Task.CompletedTask);
        }

        public RetryPolicy RetryPolicy { get; }

        public AsyncRetryPolicy AsyncRetryPolicy { get; }

        private TimeSpan DelayRoutine(Exception ex, int retries)
        {
            var waitTime = TimeSpan.FromMilliseconds(retries * config.ShortDelay);
            if (ex is RedisConnectionException)
            {
                waitTime = TimeSpan.FromMilliseconds(config.LongDelay);
            }

            logger.LogError(ex, "Redis error detected ({1}). Waiting {0}...", waitTime, ex.Message);
            return waitTime;
        }
    }
}
