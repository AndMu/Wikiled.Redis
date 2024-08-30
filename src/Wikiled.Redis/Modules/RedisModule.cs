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

namespace Wikiled.Redis.Modules
{
    public static class RedisModule
    {
        public static IServiceCollection AddRedis(this IServiceCollection services, IRedisConfiguration redisConfig, ResilienceConfig? resilienceConfig = null, bool openOnConstruction = true)
        {
            services.AddSingleton(redisConfig);
            services.AddSingleton<IResilience, ResilienceHandler>();
            services.AddSingleton<IEntitySubscriber, EntitySubscriber>();
            services.AddSingleton<IDataSerializer>(ctx => new FlatProtoDataSerializer(false, ctx.GetService<RecyclableMemoryStreamManager>()));
            resilienceConfig ??= new ResilienceConfig { LongDelay = 1000, ShortDelay = 100 };

            services.AddSingleton(resilienceConfig);

            services.AddTransient<RedisLink>();

            async Task<IRedisLink> ImplementationFactory(IServiceProvider ctx)
            {
                var logger = ctx.GetRequiredService<ILogger<RedisLink>>();
                logger.LogInformation("Redis Link Initialisation");
                var link = ctx.GetRequiredService<RedisLink>();
                if (openOnConstruction)
                {
                    logger.LogInformation("Open On Construction");
                    await ctx.GetRequiredService<IResilience>().AsyncRetryPolicy.ExecuteAsync(link.Open).ConfigureAwait(false);
                }

                return link;
            }

            services.AddAsyncFactory(ImplementationFactory);
            services.AddTransient<IRedisMultiplexer, RedisMultiplexer>();

            services.AddTransient<Func<ConfigurationOptions, Task<IConnectionMultiplexer>>>(
                ctx => async option => await ConnectionMultiplexer.ConnectAsync(option).ConfigureAwait(false));

            services.AddSingleton<Func<IRedisConfiguration, IRedisMultiplexer>>(
                ctx =>
                {
                    var logger = ctx.GetRequiredService<ILogger<RedisLink>>();
                    IRedisMultiplexer Construct(IRedisConfiguration config)
                    {
                        logger.LogInformation("Constructing: {0}", config);
                        return new RedisMultiplexer(ctx.GetRequiredService<ILogger<RedisMultiplexer>>(),
                                                    config,
                                                    ctx.GetRequiredService<Func<ConfigurationOptions, Task<IConnectionMultiplexer>>>());
                    }

                    return Construct;
                });

            return services;
        }
    }
}
