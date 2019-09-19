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

        public HandlingDefinition<T> RegisterNormalized<T>(IRedisLink link, IDataSerializer serializer = null)
            where T : class
        {
            log.LogInformation("RegisterNormalized<{0}>", typeof(T));
            var definition = ConstructGeneric<T>(link, serializer);
            definition.IsNormalized = true;
            definition.IsWellKnown = true;
            link.RegisterDefinition(definition);
            return definition;
        }

        public HandlingDefinition<T> RegisterKnownType<T>(IRedisLink link, IDataSerializer serializer = null)
            where T : class
        {
            log.LogInformation("RegisterKnownType<{0}>", typeof(T));
            var definition = ConstructGeneric<T>(link, serializer);
            definition.IsWellKnown = true;
            link.RegisterDefinition(definition);
            return definition;
        }

        public HandlingDefinition<T> RegisterHashType<T>(IRedisLink link, IKeyValueSerializer<T> serializer = null)
            where T : class, new()
        {
            log.LogInformation("RegisterHashType<{0}>", typeof(T));
            serializer = serializer ?? new KeyValueSerializer<T>(() => new T());
            var definition = ConstructGeneric<T>(link);
            definition.Serializer = serializer;
            definition.IsWellKnown = true;
            definition.IsNormalized = true;
            link.RegisterDefinition(definition);
            return definition;
        }

        public HandlingDefinition<T> RegisterGeneric<T>(IRedisLink link, IDataSerializer serializer = null)
            where T : class
        {
            log.LogInformation("ConstructGeneric<{0}>", typeof(T));
            var definition = ConstructGeneric<T>(link, serializer);
            link.RegisterDefinition(definition);
            return definition;
        }
    }
}
