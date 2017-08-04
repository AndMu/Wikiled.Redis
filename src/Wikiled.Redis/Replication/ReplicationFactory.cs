using System;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Redis.Config;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Replication
{
    public class ReplicationFactory : IReplicationFactory
    {
        private readonly IRedisFactory redisFactory;

        private readonly IScheduler scheduler;

        private readonly ILogginProgressTracker tracker;

        public ReplicationFactory(IRedisFactory redisFactory, IScheduler scheduler, ILogginProgressTracker tracker = null)
        {
            Guard.NotNull(() => redisFactory, redisFactory);
            Guard.NotNull(() => scheduler, scheduler);
            this.redisFactory = redisFactory;
            this.scheduler = scheduler;
            this.tracker = tracker;
        }

        public IReplicationManager StartReplicationFrom(IRedisMultiplexer master, IRedisMultiplexer slave)
        {
            Guard.NotNull(() => master, master);
            Guard.NotNull(() => slave, slave);
            var timer = Observable.Interval(TimeSpan.FromSeconds(1), scheduler);
            var manager = new ReplicationManager(master, slave, timer);
            tracker?.Track(manager.Progress);
            manager.Open();
            return manager;
        }

        public async Task<ReplicationProgress> Replicate(DnsEndPoint master, DnsEndPoint slave, CancellationToken token)
        {
            using (var masterMultiplexer = redisFactory.Create(new RedisConfiguration(master)))
            using (var slaveEMultiplexer = redisFactory.Create(new RedisConfiguration(slave)))
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

