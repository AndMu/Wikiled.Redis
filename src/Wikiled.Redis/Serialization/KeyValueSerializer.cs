using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Wikiled.Common.Logging;
using Wikiled.Common.Reflection;

namespace Wikiled.Redis.Serialization
{
    /// <summary>
    ///     Simple and fast serializer into key value pairs
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class KeyValueSerializer<T> : IKeyValueSerializer<T>
        where T : class, new()
    {
        private static readonly ILogger log = ApplicationLogging.CreateLogger("KeyValueSerializer");

        private readonly List<Func<T, KeyValuePair<string, string>>> readActions = new List<Func<T, KeyValuePair<string, string>>>();

        private readonly Dictionary<string, Action<KeyValuePair<string, string>, T>> writeActions =
            new Dictionary<string, Action<KeyValuePair<string, string>, T>>();

        public KeyValueSerializer()
        {
            BuildTypeMap();
        }

        public string[] Properties { get; private set; }

        public T Deserialize(IEnumerable<KeyValuePair<string, string>> entries)
        {
            return DeserializeStream(entries).First();
        }

        public IEnumerable<T> DeserializeStream(IEnumerable<KeyValuePair<string, string>> entries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            int total = 0;
            T instance = null;
            foreach(var hashEntry in entries)
            {
                if(total == 0)
                {
                    instance = new T();
                }

                total++;
                if (writeActions.TryGetValue(hashEntry.Key, out Action<KeyValuePair<string, string>, T> setAction))
                {
                    setAction(hashEntry, instance);
                }
                else
                {
                    log.LogError("Failed to find entry: {0} in instance: {1}", hashEntry.Key, typeof(T));
                }

                if (total == Properties.Length)
                {
                    total = 0;
                    if(instance != null)
                    {
                        yield return instance;
                    }
                }
            }
        }

        public IEnumerable<KeyValuePair<string, string>> Serialize(T instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            return readActions.Select(item => item(instance));
        }

        private void BuildTypeMap()
        {
            var type = typeof(T);
            foreach(var property in type.GetProperties())
            {
                var currentProperty = property;
                var getter = property.GetValueGetter<T>();
                var isStringType = currentProperty.PropertyType == typeof(string);
                Func<T, KeyValuePair<string, string>> read = instance =>
                {
                    var value = getter(instance);
                    return new KeyValuePair<string, string>(currentProperty.Name, value);
                };

                readActions.Add(read);
                var setter = currentProperty.GetValueSetter<T, object>();
                var parser = ReflectionExtension.GetParser(currentProperty.PropertyType);
                Action<KeyValuePair<string, string>, T> write = (entry, instance) =>
                {
                    if(isStringType)
                    {
                        setter(instance, entry.Value);
                    }
                    else if(!string.IsNullOrEmpty(entry.Value))
                    {
                        setter(instance, parser(entry.Value));
                    }
                };

                writeActions.Add(property.Name, write);
            }

            Properties = writeActions.Keys.ToArray();
        }
    }
}
