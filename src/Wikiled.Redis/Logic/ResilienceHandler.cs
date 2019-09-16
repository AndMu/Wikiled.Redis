using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;

namespace Wikiled.Redis.Logic
{
    public class ResilienceHandler : IResilience
    {
        public ResilienceHandler(ILogger<ResilienceHandler> logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            RetryPolicy = Policy
                          .Handle<Exception>()
                          .Retry(3, (exception, i) => logger.LogError(exception, $"Failed {i} attempt"));

            AsyncRetryPolicy = Policy
                               .Handle<Exception>()
                               .RetryAsync(3, (exception, i) => logger.LogError(exception, $"Failed {i} attempt"));
        }

        public RetryPolicy RetryPolicy { get; }

        public AsyncRetryPolicy AsyncRetryPolicy { get; }
    }
}
