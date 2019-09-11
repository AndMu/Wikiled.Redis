using System;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Wikiled.Redis.Config;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Replication
{
    public class ReplicationFactory : IReplicationFactory
    {
        private readonly Func<IRedisConfiguration, IRedisMultiplexer> redisFactory;

        private readonly IScheduler scheduler;

        private readonly ILoggingProgressTracker tracker;

        public ReplicationFactory(Func<IRedisConfiguration, IRedisMultiplexer> redisFactory, IScheduler scheduler, ILoggingProgressTracker tracker = null)
        {
            this.redisFactory = redisFactory ?? throw new ArgumentNullException(nameof(redisFactory));
            this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            this.tracker = tracker;
        }

        public IReplicationManager StartReplicationFrom(IRedisMultiplexer master, IRedisMultiplexer slave)
        {
            if (master == null)
            {
                throw new ArgumentNullException(nameof(master));
            }

            if (slave == null)
            {
                throw new ArgumentNullException(nameof(slave));
            }

            var timer = Observable.Interval(TimeSpan.FromSeconds(1), scheduler);
            var manager = new ReplicationManager(master, slave, timer);
            tracker?.Track(manager.Progress);
            manager.Open();
            return manager;
        }

        public async Task<ReplicationProgress> Replicate(DnsEndPoint master, DnsEndPoint slave, CancellationToken token)
        {
            using (var masterMultiplexer = redisFactory(new RedisConfiguration(master)))
            using (var slaveEMultiplexer = redisFactory(new RedisConfiguration(slave)))
            {
                masterMultiplexer.Open();
                slaveEMultiplexer.Open();

                var timer = Observable.Interval(TimeSpan.FromSeconds(1), scheduler);
                ReplicationProgress result;
                using (var manager = new ReplicationManager(masterMultiplexer, slaveEMultiplexer, timer))
                {
                    tracker?.Track(manager.Progress);
                    manager.Open();
                    result = await manager.Progress
                                          .Where(item => item.InSync)
                                          .FirstAsync()
                                          .ToTask(token)
                                          .ConfigureAwait(false);
                }

                masterMultiplexer.Close();
                slaveEMultiplexer.Close();
                return result;
            }
        }
    }
}

