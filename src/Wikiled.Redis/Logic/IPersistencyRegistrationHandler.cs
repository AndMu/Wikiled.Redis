using Wikiled.Redis.Data;
using Wikiled.Redis.Serialization;

namespace Wikiled.Redis.Logic
{
    public interface IPersistencyRegistrationHandler
    {
        void RegisterList<T>(IDataSerializer serializer);

        void RegisterSet<T>(IDataSerializer serializer);

        void RegisterHashsetSingle<T>(IKeyValueSerializer<T> serializer = null)
            where T : class, new();

        void RegisterHashSet<T>(IKeyValueSerializer<T> serializer = null)
            where T : class, new();

        void RegisterObjectHashSingle<T>(IDataSerializer serializer, bool isWellKnown = false)
            where T : class;

        void RegisterObjectHashSet<T>(IDataSerializer serializer, bool isWellKnown = false)
            where T : class;
    }
}