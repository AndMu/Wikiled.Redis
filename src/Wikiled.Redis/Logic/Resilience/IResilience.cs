using Polly.Retry;

namespace Wikiled.Redis.Logic.Resilience
{
    public interface IResilience
    {
        RetryPolicy RetryPolicy { get; }

        AsyncRetryPolicy AsyncRetryPolicy { get; }
    }
}
