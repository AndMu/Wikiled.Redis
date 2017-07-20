using System;
using System.Threading;
using NLog;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Data;
using Wikiled.Redis.Serialization;

namespace Wikiled.Redis.Logic
{
    public class HandlingDefinition<T> : IHandlingDefinition
    {
        private static readonly Logger log = LogManager.GetLogger($"HandlingDefinition<{typeof(T)}>");

        private long counter;

        private bool extractType;

        private bool isSingleInstance;

        private long linkId;

        private HandlingDefinition()
        {
        }

        public IDataSerializer DataSerializer { get; private set; }

        public bool ExtractType
        {
            get => extractType;
            set
            {
                if(value &&
                   RedisValueExtractor.IsPrimitive<T>())
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Primitive type can't extract type");
                }

                extractType = value;
            }
        }

        public bool IsSet { get; set; }

        /// <summary>
        ///     Is given type persisted as single type
        /// </summary>
        public bool IsSingleInstance
        {
            get => isSingleInstance;
            set
            {
                if(value &&
                   RedisValueExtractor.IsPrimitive<T>())
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Primitive type can't have single instance");
                }

                isSingleInstance = value;
            }
        }

        /// <summary>
        ///     Is well known
        /// </summary>
        public bool IsWellKnown { get; private set; }

        /// <summary>
        ///     Get key value serializer
        /// </summary>
        /// <typeparam name="T">Type serializer</typeparam>
        /// <returns>If it is supported instance of serializer</returns>
        public IKeyValueSerializer<T> Serializer { get; private set; }

        public static HandlingDefinition<T> ConstructGeneric(IRedisLink redis, IDataSerializer serializer = null)
        {
            Guard.IsValid(() => redis, redis, item => redis.State == ChannelState.Open, "Redis link is not open");
            Guard.IsValid(() => redis, redis, item => item.LinkId >= 0, "Redis link id invalid");
            log.Debug("ConstructGeneric");
            if(RedisValueExtractor.IsPrimitive<T>() &&
               (serializer != null))
            {
                throw new ArgumentOutOfRangeException(nameof(T), "Primitive type can't have serializer");
            }

            if((serializer == null) &&
               !RedisValueExtractor.IsPrimitive<T>())
            {
                serializer = new FlatProtoDataSerializer(false);
            }

            var instance = new HandlingDefinition<T>();
            instance.IsWellKnown = false;
            instance.linkId = redis.LinkId;
            instance.DataSerializer = serializer;
            if (typeof(T) == typeof(PrimitiveSet))
            {
                instance.IsSet = true;
                instance.DataSerializer = new PrimitiveSerializer();
            }

            return instance;
        }

        public static HandlingDefinition<T> ConstructKeyValue(IRedisLink redis, IKeyValueSerializer<T> serializer)
        {
            Guard.IsValid(() => redis, redis, item => redis.State == ChannelState.Open, "Redis link is not open");
            Guard.IsValid(() => redis, redis, item => item.LinkId >= 0, "Redis link id invalid");
            Guard.NotNull(() => serializer, serializer);
            log.Debug("ConstructKeyValue");
            var instance = new HandlingDefinition<T>();
            if(RedisValueExtractor.IsPrimitive<T>())
            {
                throw new ArgumentOutOfRangeException(nameof(T), "Primitive type can't be key value");
            }

            instance.IsWellKnown = true;
            instance.Serializer = serializer;
            instance.linkId = redis.LinkId;
            return instance;
        }

        public static HandlingDefinition<T> ConstructWellKnown(IRedisLink redis, IDataSerializer serializer = null)
        {
            Guard.IsValid(() => redis, redis, item => redis.State == ChannelState.Open, "Redis link is not open");
            Guard.IsValid(() => redis, redis, item => item.LinkId >= 0, "Redis link id invalid");
            log.Debug("ConstructWellKnown");
            if(RedisValueExtractor.IsPrimitive<T>())
            {
                throw new ArgumentOutOfRangeException(nameof(T), "Primitive type can't be well known");
            }

            if(serializer == null)
            {
                serializer = new FlatProtoDataSerializer(true);
            }

            var instance = new HandlingDefinition<T>();
            instance.IsWellKnown = true;
            instance.linkId = redis.LinkId;
            instance.DataSerializer = serializer;
            return instance;
        }

        /// <summary>
        ///     Get Next id for the given type
        /// </summary>
        /// <returns></returns>
        public string GetNextId()
        {
            return $"L{linkId}:{Interlocked.Increment(ref counter)}";
        }
    }
}
