using Wikiled.Redis.Data;
using Wikiled.Redis.Serialization;

namespace Wikiled.Redis.Logic
{
    public interface IHandlingDefinitionFactory
    {
        HandlingDefinition<T> ConstructGeneric<T>(IRedisLink redis, IDataSerializer serializer = null);

        HandlingDefinition<T> RegisterNormalized<T>(IRedisLink link, IDataSerializer serializer = null)
            where T : class;

        HandlingDefinition<T> RegisterKnownType<T>(IRedisLink link, IDataSerializer serializer = null)
            where T : class;

        HandlingDefinition<T> RegisterHashType<T>(IRedisLink link, IKeyValueSerializer<T> serializer = null)
            where T : class, new();

        HandlingDefinition<T> RegisterGeneric<T>(IRedisLink link, IDataSerializer serializer = null)
            where T : class;
    }
}