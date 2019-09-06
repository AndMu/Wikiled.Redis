using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Wikiled.Redis.Replication
{
    public class LoggingProgressTracker : ILoggingProgressTracker
    {
        private readonly IScheduler scheduler;

        private readonly TimeSpan frequency;

        private readonly Action<string> logging;

        public LoggingProgressTracker(IScheduler scheduler, TimeSpan frequency, Action<string> logging)
        {
            this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            this.frequency = frequency;
            this.logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        public void Track(IObservable<ReplicationProgress> progress)
        {
            progress.Throttle(frequency, scheduler).Subscribe(Track);
        }

        private void Track(ReplicationProgress progress)
        {
            if (progress == null)
            {
                return;
            }

            foreach (var record in progress.GenerateProgress())
            {
                logging(record);
            }
        }
    }
}
