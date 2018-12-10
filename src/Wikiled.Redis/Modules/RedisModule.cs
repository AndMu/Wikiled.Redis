using System;
using Autofac;
using Microsoft.Extensions.Logging;
using Wikiled.Common.Logging;
using Wikiled.Redis.Config;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Modules
{
    public class RedisModule : Module
    {
        private ILogger log = ApplicationLogging.CreateLogger<RedisModule>();

        public RedisModule(string name, RedisConfiguration redisConfiguration)
        {
            RedisConfiguration = redisConfiguration ?? throw new ArgumentNullException(nameof(redisConfiguration));
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string Name { get; }

        public RedisConfiguration RedisConfiguration { get; }

        protected override void Load(ContainerBuilder builder)
        {
            log.LogDebug("Using Redis cache");
            builder.Register(c => new RedisLink(Name, new RedisMultiplexer(RedisConfiguration)))
                .OnActivating(item => item.Instance.Open());
        }
    }
}
