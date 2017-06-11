using System;
using Polly;
using Polly.Retry;
using StackExchange.Redis;

namespace Wikiled.Redis.Logic
{
    public static class RetryHandler
    {
        public static RetryPolicy Construct(bool async = false)
        {
            var policy = Policy
                .Handle<RedisException>()
                .Or<RedisCommandException>();
            return async ?
                policy
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                ) :
                policy
                .WaitAndRetry(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                );
        }
    }
}
