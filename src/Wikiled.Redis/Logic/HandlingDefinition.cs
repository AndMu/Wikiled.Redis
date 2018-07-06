using System;
using System.Threading;
using NLog;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Data;
using Wikiled.Redis.Serialization;

namespace Wikiled.Redis.Logic
{
    public class HandlingDefinition<T>
    {
        private static readonly Logger log = LogManager.GetLogger($"HandlingDefinition<{typeof(T)}>");

        private long counter;

        private bool extractType;

        private bool isSingleInstance;

        private long linkId;

        private bool isWellKnown;

        private IKeyValueSerializer<T> serializer;

        private bool isNormalized;

        private HandlingDefinition()
        {
        }

        public IDataSerializer DataSerializer { get; private set; }

        public bool ExtractType
        {
            get => extractType;
            set
            {
                if (value &&
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
                if (value &&
                   RedisValueExtractor.IsPrimitive<T>())
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Primitive type can't have single instance");
                }

                isSingleInstance = value;
            }
        }

        public bool IsNormalized
        {
            get => isNormalized;
            set
            {
                if (value &&
                    RedisValueExtractor.IsPrimitive<T>())
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Primitive type can't be normalized");
                }

                isNormalized = value;
            }
        }

        /// <summary>
        ///     Is well known
        /// </summary>
        public bool IsWellKnown
        {
            get => isWellKnown;
            set
            {
                if (value &&
                    RedisValueExtractor.IsPrimitive<T>())
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Primitive type can't be well known ");
                }

                isWellKnown = value;
            }
        }

        /// <summary>
        ///     Get key value serializer
        /// </summary>
        /// <typeparam name="T">Type serializer</typeparam>
        /// <returns>If it is supported instance of serializer</returns>
        public IKeyValueSerializer<T> Serializer
        {
            get => serializer;
            set
            {
                if (value != null &&
                    RedisValueExtractor.IsPrimitive<T>())
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Primitive can't have key value serializer");
                }

                serializer = value;
            }
        }

        public static HandlingDefinition<T> ConstructGeneric(IRedisLink redis, IDataSerializer serializer = null)
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

            log.Debug("ConstructGeneric");
            if (RedisValueExtractor.IsPrimitive<T>() &&
               (serializer != null))
            {
                throw new ArgumentOutOfRangeException(nameof(T), "Primitive type can't have serializer");
            }

            if ((serializer == null) &&
               !RedisValueExtractor.IsPrimitive<T>())
            {
                serializer = new FlatProtoDataSerializer(false);
            }

            var instance = new HandlingDefinition<T>();
            instance.IsWellKnown = false;
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
