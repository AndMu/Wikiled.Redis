using Microsoft.Extensions.DependencyInjection;
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
        public RedisModule(RedisConfiguration redisConfiguration)
        {
            RedisConfiguration = redisConfiguration ?? throw new ArgumentNullException(nameof(redisConfiguration));
        }

        public RedisConfiguration RedisConfiguration { get; }

        public ResilienceConfig ResilienceConfig { get; set; } = new ResilienceConfig { LongDelay = 1000, ShortDelay = 100 };

        public bool IsSingleInstance { get; set; }

        public bool OpenOnConstruction { get; set; } = true;

        public IServiceCollection ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IRedisConfiguration>(RedisConfiguration);
            services.AddSingleton<IResilience, ResilienceHandler>();
            services.AddSingleton<IEntitySubscriber, EntitySubscriber>();
            services.AddSingleton<IDataSerializer>(ctx => new FlatProtoDataSerializer(false, ctx.GetService<RecyclableMemoryStreamManager>()));
            services.AddSingleton(ResilienceConfig);

            services.AddTransient<RedisLink>();

            async Task<IRedisLink> ImplementationFactory(IServiceProvider ctx)
            {
                var logger = ctx.GetRequiredService<ILogger<RedisModule>>();
                logger.LogInformation("Redis Link Initialisation");
                var link = ctx.GetService<RedisLink>();
                if (OpenOnConstruction)
                {
                    logger.LogInformation("Open On Construction");
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

            services.AddTransient<Func<ConfigurationOptions, Task<IConnectionMultiplexer>>>(
                ctx => async option => await ConnectionMultiplexer.ConnectAsync(option).ConfigureAwait(false));
            services.AddTransient<IReplicationFactory, ReplicationFactory>();

            services.AddSingleton<Func<IRedisConfiguration, IRedisMultiplexer>>(
                ctx =>
                {
                    var logger = ctx.GetRequiredService<ILogger<RedisModule>>();
                    IRedisMultiplexer Construct(IRedisConfiguration config)
                    {
                        logger.LogInformation("Constructing: {0}", config);
                        return new RedisMultiplexer(ctx.GetService<ILogger<RedisMultiplexer>>(),
                                                    config,
                                                    ctx.GetService<Func<ConfigurationOptions, Task<IConnectionMultiplexer>>>());
                    }

                    return Construct;
                });

            return services;
        }
    }
}
