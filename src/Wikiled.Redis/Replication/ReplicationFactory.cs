﻿using System;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wikiled.Redis.Config;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Replication
{
    public class ReplicationFactory : IReplicationFactory
    {
        private readonly Func<IRedisConfiguration, IRedisMultiplexer> redisFactory;

        private readonly IScheduler scheduler;

        private readonly ILoggingProgressTracker tracker;

        private readonly ILoggerFactory loggerFactory;

        public ReplicationFactory(ILoggerFactory loggerFactory, Func<IRedisConfiguration, IRedisMultiplexer> redisFactory, IScheduler scheduler, ILoggingProgressTracker tracker = null)
        {
            this.redisFactory = redisFactory ?? throw new ArgumentNullException(nameof(redisFactory));
            this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            this.tracker = tracker;
        }

        public async Task<IReplicationManager> StartReplicationFrom(IRedisMultiplexer master, IRedisMultiplexer slave)
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
            var manager = new ReplicationManager(loggerFactory.CreateLogger<ReplicationManager>(), master, slave, timer);
            tracker?.Track(manager.Progress);
            await manager.Open().ConfigureAwait(false);
            return manager;
        }

        public async Task<ReplicationProgress> Replicate(DnsEndPoint master, DnsEndPoint slave, CancellationToken token)
        {
            using (var masterMultiplexer = redisFactory(new RedisConfiguration(master)))
            using (var slaveEMultiplexer = redisFactory(new RedisConfiguration(slave)))
            {
                await masterMultiplexer.Open().ConfigureAwait(false);
                await slaveEMultiplexer.Open().ConfigureAwait(false);

                var timer = Observable.Interval(TimeSpan.FromSeconds(1), scheduler);
                ReplicationProgress result;
                using (var manager = new ReplicationManager(loggerFactory.CreateLogger<ReplicationManager>(), masterMultiplexer, slaveEMultiplexer, timer))
                {
                    tracker?.Track(manager.Progress);
                    await manager.Open().ConfigureAwait(false);
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

