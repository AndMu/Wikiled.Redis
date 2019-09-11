using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using Wikiled.Common.Utilities.Modules;
using Wikiled.Redis.Config;
using Wikiled.Redis.Logic;
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

        public IServiceCollection ConfigureServices(IServiceCollection services)
        {
            logger.LogDebug("Using Redis cache");
            services.AddSingleton<IRedisConfiguration>(RedisConfiguration);
            services.AddTransient<IRedisLink, RedisLink>();
            services.AddTransient<IRedisMultiplexer, RedisMultiplexer>();
            services.AddTransient<IReplicationFactory, ReplicationFactory>();
            
            services.AddSingleton<Func<IRedisConfiguration, IRedisMultiplexer>>(
                x =>
                {
                    IRedisMultiplexer Construct(IRedisConfiguration config)
                    {
                        return new RedisMultiplexer(x.GetService<ILogger<RedisMultiplexer>>(), config);
                    }

                    return Construct;
                });

            return services;
        }
    }
}
