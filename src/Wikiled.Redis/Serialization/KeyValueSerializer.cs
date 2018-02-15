using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Wikiled.Common.Arguments;
using Wikiled.Common.Extensions;
using Wikiled.Common.Reflection;

namespace Wikiled.Redis.Serialization
{
    /// <summary>
    ///     Simple and fast serializer into key value pairs
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class KeyValueSerializer<T> : IKeyValueSerializer<T>
        where T : class
    {
        private readonly Func<T> factory;

        private readonly Logger log = LogManager.GetLogger("TagSerializer");

        private readonly List<Func<T, KeyValuePair<string, string>>> readActions = new List<Func<T, KeyValuePair<string, string>>>();

        private readonly Dictionary<string, Action<KeyValuePair<string, string>, T>> writeActions =
            new Dictionary<string, Action<KeyValuePair<string, string>, T>>();

        public KeyValueSerializer(Func<T> factory)
        {
            this.factory = factory;
            BuildTypeMap();
        }

        public string[] Properties { get; private set; }

        public T Deserialize(IEnumerable<KeyValuePair<string, string>> entries)
        {
            return DeserializeStream(entries).First();
        }

        public IEnumerable<T> DeserializeStream(IEnumerable<KeyValuePair<string, string>> entries)
        {
            Guard.NotNull(() => entries, entries);
            int total = 0;
            T instance = null;
            foreach(var hashEntry in entries)
            {
                if(total == 0)
                {
                    instance = factory();
                }

                total++;
                Action<KeyValuePair<string, string>, T> setAction;
                if(writeActions.TryGetValue(hashEntry.Key, out setAction))
                {
                    setAction(hashEntry, instance);
                }
                else
                {
                    log.Error("Failed to find entry: {0} in instance: {1}", hashEntry.Key, typeof(T));
                }

                if(total == Properties.Length)
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
            Guard.NotNull(() => instance, instance);
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
