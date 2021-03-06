﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.IO;
using StackExchange.Redis;
using Wikiled.Common.Utilities.Modules;
using Wikiled.Redis.Config;
using Wikiled.Redis.Data;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Logic.Resilience;
using Wikiled.Redis.Persistency;
using Wikiled.Redis.Replication;

namespace Wikiled.Redis.Modules
{
    public class RedisModule : IModule
    {
        private readonly ILogger logger;

        public RedisModule(ILogger logger, RedisConfiguration redisConfiguration)
        {
            RedisConfiguration = redisConfiguration ?? throw new ArgumentNullException(nameof(redisConfiguration));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public RedisConfiguration RedisConfiguration { get; }

        public ResilienceConfig ResilienceConfig { get; set; } = new ResilienceConfig { LongDelay = 1000, ShortDelay = 100 };

        public bool IsSingleInstance { get; set; }

        public bool OpenOnConstruction { get; set; } = true;

        public IServiceCollection ConfigureServices(IServiceCollection services)
        {
            logger.LogDebug("Using Redis cache");
            services.AddSingleton<IRedisConfiguration>(RedisConfiguration);
            services.AddSingleton<IResilience, ResilienceHandler>();
            services.AddSingleton<IEntitySubscriber, EntitySubscriber>();
            services.AddSingleton<IDataSerializer>(ctx => new FlatProtoDataSerializer(false, ctx.GetService<RecyclableMemoryStreamManager>()));
            services.AddSingleton(ResilienceConfig);

            services.AddTransient<RedisLink>();

            async Task<IRedisLink> ImplementationFactory(IServiceProvider ctx)
            {
                var link = ctx.GetService<RedisLink>();
                if (OpenOnConstruction)
                {
                    await ctx.GetService<IResilience>().AsyncRetryPolicy.ExecuteAsync(link.Open).ConfigureAwait(false);
                }

                return link;
            }

            if (IsSingleInstance)
            {
                services.AddSingleton(ImplementationFactory);
                services.AddSingleton(ctx => ctx.GetService<Task<IRedisLink>>().Result);
            }
            else
            {
                services.AddTransient(ImplementationFactory);
                services.AddTransient(ctx => ctx.GetService<Task<IRedisLink>>().Result);
            }

            services.AddFactory<IRedisLink>();
            services.AddTransient<IRedisMultiplexer, RedisMultiplexer>();

            services.AddSingleton<Func<ConfigurationOptions, Task<IConnectionMultiplexer>>>(
                ctx =>
                    async option => (await ConnectionMultiplexer.ConnectAsync(option).ConfigureAwait(false)) as IConnectionMultiplexer);
            services.AddTransient<IReplicationFactory, ReplicationFactory>();

            services.AddSingleton<Func<IRedisConfiguration, IRedisMultiplexer>>(
                x =>
                {
                    IRedisMultiplexer Construct(IRedisConfiguration config)
                    {
                        return new RedisMultiplexer(x.GetService<ILogger<RedisMultiplexer>>(),
                                                    config,
                                                    x.GetService<Func<ConfigurationOptions, Task<IConnectionMultiplexer>>>());
                    }

                    return Construct;
                });

            return services;
        }
    }
}
