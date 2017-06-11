using System.Collections.Generic;

namespace Wikiled.Redis.Serialization
{
    public interface IKeyValueSerializer<T>
    {
        string[] Properties { get; }

        T Deserialize(IEnumerable<KeyValuePair<string, string>> entries);

        IEnumerable<T> DeserializeStream(IEnumerable<KeyValuePair<string, string>> entries);

        IEnumerable<KeyValuePair<string, string>> Serialize(T instance);
    }
}
