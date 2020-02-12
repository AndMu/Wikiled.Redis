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
        private readonly ILogger<KeyValueSerializer<T>> log;

        private readonly List<Func<T, KeyValuePair<string, string>>> readActions = new List<Func<T, KeyValuePair<string, string>>>();

        private readonly Dictionary<string, Action<KeyValuePair<string, string>, T>> writeActions =
            new Dictionary<string, Action<KeyValuePair<string, string>, T>>();

        public KeyValueSerializer(ILogger<KeyValueSerializer<T>> log)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
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
            foreach (var hashEntry in entries)
            {
                if (total == 0)
                {
                    instance = new T();
                }

                total++;

                try
                {
                    if (writeActions.TryGetValue(hashEntry.Key, out Action<KeyValuePair<string, string>, T> setAction))
                    {
                        setAction(hashEntry, instance);
                    }
                    else
                    {
                        log.LogError("Failed to find entry: {0} in instance: {1}", hashEntry.Key, typeof(T));
                    }
                }
                catch (Exception e)
                {
                    log.LogError(e, "Property serializstion error");
                }

                if (total != Properties.Length)
                {
                    continue;
                }

                total = 0;
                if (instance != null)
                {
                    yield return instance;
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
            foreach (var property in type.GetProperties())
            {
                var currentProperty = property;
                var getter = property.GetValueGetter<T>();
                var isStringType = currentProperty.PropertyType == typeof(string);

                KeyValuePair<string, string> Read(T instance)
                {
                    var value = getter(instance);

                    return new KeyValuePair<string, string>(currentProperty.Name, value);
                }

                readActions.Add(Read);
                var setter = currentProperty.GetValueSetter<T, object>();
                var parser = ReflectionExtension.GetParser(currentProperty.PropertyType);

                void Write(KeyValuePair<string, string> entry, T instance)
                {
                    if (isStringType)
                    {
                        setter(instance, entry.Value);
                    }
                    else if (!string.IsNullOrEmpty(entry.Value))
                    {
                        setter(instance, parser(entry.Value));
                    }
                }

                writeActions.Add(property.Name, Write);
            }

            Properties = writeActions.Keys.ToArray();
        }
    }
}
