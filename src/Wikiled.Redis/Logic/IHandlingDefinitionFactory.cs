using Wikiled.Redis.Data;

namespace Wikiled.Redis.Logic
{
    public interface IHandlingDefinitionFactory
    {
        HandlingDefinition<T> ConstructGeneric<T>(IRedisLink redis, IDataSerializer serializer = null);
    }
}