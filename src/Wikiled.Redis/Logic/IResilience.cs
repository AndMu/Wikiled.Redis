using Polly.Retry;

namespace Wikiled.Redis.Logic
{
    public interface IResilience
    {
        RetryPolicy RetryPolicy { get; }

        AsyncRetryPolicy AsyncRetryPolicy { get; }
    }
}
