using Microsoft.Extensions.Logging;
using Microsoft.IO;
using System;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Data;
using Wikiled.Redis.Serialization;

namespace Wikiled.Redis.Logic
{
    public class HandlingDefinitionFactory : IHandlingDefinitionFactory
    {
        private readonly ILogger<HandlingDefinitionFactory> log;

        private readonly RecyclableMemoryStreamManager memoryStreamManager;

        public HandlingDefinitionFactory(ILogger<HandlingDefinitionFactory> log, RecyclableMemoryStreamManager memoryStreamManager)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.memoryStreamManager = memoryStreamManager ?? throw new ArgumentNullException(nameof(memoryStreamManager));
        }

        public HandlingDefinition<T> ConstructGeneric<T>(IRedisLink redis, IDataSerializer serializer = null)
        {
            if (redis == null)
            {
                throw new ArgumentNullException(nameof(redis));
            }

            if (redis.State != ChannelState.Open)
            {
                throw new ArgumentOutOfRangeException("Redis link is not open", nameof(redis));
            }

            if (redis.LinkId < 0)
            {
                throw new ArgumentOutOfRangeException("Redis link id invalid", nameof(redis));
            }

            log.LogDebug("ConstructGeneric");
            if (RedisValueExtractor.IsPrimitive<T>() &&
                (serializer != null))
            {
                throw new ArgumentOutOfRangeException(nameof(T), "Primitive type can't have serializer");
            }

            if ((serializer == null) &&
                !RedisValueExtractor.IsPrimitive<T>())
            {
                serializer = new FlatProtoDataSerializer(false, memoryStreamManager);
            }

            var instance = new HandlingDefinition<T>(redis.LinkId, serializer);
            instance.IsWellKnown = false;
            return instance;
        }
    }
}
