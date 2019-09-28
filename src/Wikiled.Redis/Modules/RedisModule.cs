using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using StackExchange.Redis;
using Wikiled.Common.Utilities.Modules;
using Wikiled.Redis.Config;
using Wikiled.Redis.Logic;
using Wikiled.Redis.Logic.Resilience;
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

        public ResilienceConfig ResilienceConfig { get; set; } = new ResilienceConfig() {LongDelay = 1000, ShortDelay = 100};

        public bool IsSingleInstance { get; set; }

        public bool OpenOnConstruction { get; set; } = true;

        public IServiceCollection ConfigureServices(IServiceCollection services)
        {
            logger.LogDebug("Using Redis cache");
            services.AddSingleton<IRedisConfiguration>(RedisConfiguration);
            services.AddSingleton<IResilience, ResilienceHandler>();
            services.AddSingleton<IHandlingDefinitionFactory, HandlingDefinitionFactory>();
            services.AddSingleton(ResilienceConfig);
            
            services.AddTransient<RedisLink>();

            IRedisLink ImplementationFactory(IServiceProvider ctx)
            {
                var link = ctx.GetService<RedisLink>();
                if (OpenOnConstruction)
                {
                    ctx.GetService<IResilience>().RetryPolicy.Execute(link.Open);
                }

                return link;
            }

            if (IsSingleInstance)
            {
                services.AddSingleton(ImplementationFactory);
            }
            else
            {
                services.AddTransient(ImplementationFactory);
            }

            services.AddFactory<IRedisLink>();

            services.AddTransient<IRedisMultiplexer, RedisMultiplexer>();
            services.AddSingleton<Func<ConfigurationOptions, IConnectionMultiplexer>>(ctx => option => ConnectionMultiplexer.Connect(option) as IConnectionMultiplexer);
            services.AddTransient<IReplicationFactory, ReplicationFactory>();
            
            services.AddSingleton<Func<IRedisConfiguration, IRedisMultiplexer>>(
                x =>
                {
                    IRedisMultiplexer Construct(IRedisConfiguration config)
                    {
                        return new RedisMultiplexer(x.GetService<ILogger<RedisMultiplexer>>(),
                                                    config,
                                                    options => ConnectionMultiplexer.Connect(options));
                    }

                    return Construct;
                });

            return services;
        }
    }
}
