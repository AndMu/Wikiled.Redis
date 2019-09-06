using System;

namespace Wikiled.Redis.Replication
{
    public interface ILoggingProgressTracker
    {
        void Track(IObservable<ReplicationProgress> progress);
    }
}