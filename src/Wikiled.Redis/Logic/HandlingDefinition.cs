using System;
using System.Threading;
using Wikiled.Redis.Data;
using Wikiled.Redis.Serialization;

namespace Wikiled.Redis.Logic
{
    public class HandlingDefinition<T>
    {
        private long counter;

        private bool extractType;

        private bool isSingleInstance;

        private readonly long linkId;

        private bool isWellKnown;

        private IKeyValueSerializer<T> serializer;

        private bool isNormalized;

        public HandlingDefinition(long linkId, IDataSerializer dataSerializer)
        {
            this.linkId = linkId;
            DataSerializer = dataSerializer ?? throw new ArgumentNullException(nameof(dataSerializer));
        }

        public IDataSerializer DataSerializer { get; }

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
