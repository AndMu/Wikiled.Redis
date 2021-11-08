using System;
using System.Collections.Generic;
using StackExchange.Redis;

namespace Wikiled.Redis.Data
{
    public class RedisValueExtractor
    {
        private static readonly Dictionary<Type, bool> primitive = new Dictionary<Type, bool>
        {
            {typeof(int), true},
            {typeof(int?), true},
            {typeof(long), true},
            {typeof(long?), true},
            {typeof(double), true},
            {typeof(double?), true},
            {typeof(bool), true},
            {typeof(byte[]), true},
            {typeof(bool?), true},
            {typeof(string), true},
        };

        public static bool IsPrimitive<T>()
        {
            return primitive.ContainsKey(typeof(T));
        }

        public static bool TryParsePrimitive<T>(T instance, out RedisValue value)
        {
            value = default;
            if ((instance != null && !primitive.ContainsKey(instance.GetType())) ||
                (instance == null && !IsPrimitive<T>()))
            {
                return false;
            }

            if (instance == null)
            {
                value = RedisValue.Null;
            }
            else if (typeof(T) == typeof(int))
            {
                value = (int)(object)instance;
            }
            else if (typeof(T) == typeof(long))
            {
                value = (long)(object)instance;
            }
            else if (typeof(T) == typeof(double))
            {
                value = (double)(object)instance;
            }
            else if (typeof(T) == typeof(byte[]))
            {
                value = (byte[])(object)instance;
            }
            else if (typeof(T) == typeof(bool))
            {
                value = (bool)(object)instance;
            }
            else
            {
                value = instance.ToString();
            }

            return true;
        }

        public static T SafeConvert<T>(RedisValue value)
        {
            if (value == RedisValue.Null)
            {
                return default(T);
            }

            if (!IsPrimitive<T>())
            {
                throw new InvalidOperationException("Not supported for non primitive types");
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }
    }
}
