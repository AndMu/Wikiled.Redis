using NLog;
using Wikiled.Redis.Data;
using Wikiled.Redis.Serialization;

namespace Wikiled.Redis.Logic
{
    public static class RedisLinkExtensions
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public static HandlingDefinition<T> RegisterNormalized<T>(this IRedisLink link, IDataSerializer serializer = null)
            where T : class
        {
            log.Info("RegisterNormalized<{0}>", typeof(T));
            var definition = HandlingDefinition<T>.ConstructGeneric(link, serializer);
            definition.IsNormalized = true;
            link.RegisterDefinition(definition);
            return definition;
        }

        public static HandlingDefinition<T> RegisterKnownType<T>(this IRedisLink link, IDataSerializer serializer = null)
            where T : class
        {
            log.Info("RegisterKnownType<{0}>", typeof(T));
            var definition = HandlingDefinition<T>.ConstructGeneric(link, serializer);
            definition.IsWellKnown = true;
            link.RegisterDefinition(definition);
            return definition;
        }

        public static HandlingDefinition<T> RegisterHashType<T>(this IRedisLink link, IKeyValueSerializer<T> serializer = null)
            where T : class, new()
        {
            log.Info("RegisterHashType<{0}>", typeof(T));
            serializer = serializer ?? new KeyValueSerializer<T>(() => new T());
            var definition = HandlingDefinition<T>.ConstructGeneric(link);
            definition.Serializer = serializer;
            definition.IsWellKnown = true;
            definition.IsNormalized = true;
            link.RegisterDefinition(definition);
            return definition;
        }

        public static HandlingDefinition<T> RegisterGeneric<T>(this IRedisLink link, IDataSerializer serializer = null)
            where T : class
        {
            log.Info("ConstructGeneric<{0}>", typeof(T));
            var definition = HandlingDefinition<T>.ConstructGeneric(link, serializer);
            link.RegisterDefinition(definition);
            return definition;
        }
    }
}
