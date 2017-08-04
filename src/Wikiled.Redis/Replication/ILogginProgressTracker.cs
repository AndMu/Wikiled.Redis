using System;

namespace Wikiled.Redis.Replication
{
    public interface ILogginProgressTracker
    {
        void Track(IObservable<ReplicationProgress> progress);
    }
}