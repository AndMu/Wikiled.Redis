using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using NLog;
using Wikiled.Core.Utility.Arguments;

namespace Wikiled.Redis.Replication
{
    public class LogginProgressTracker : ILogginProgressTracker
    {
        private readonly IScheduler scheduler;

        private readonly TimeSpan frequency;

        private readonly Action<string> logging;

        public LogginProgressTracker(IScheduler scheduler, TimeSpan frequency, Action<string> logging)
        {
            Guard.NotNull(() => scheduler, scheduler);
            Guard.NotNull(() => logging, logging);
            this.scheduler = scheduler;
            this.frequency = frequency;
            this.logging = logging;
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
