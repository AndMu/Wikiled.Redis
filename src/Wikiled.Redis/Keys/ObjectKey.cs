using System;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Keys
{
    public class ObjectKey : BaseKey
    {
        public ObjectKey(string id)
            : base(id, FieldConstants.Object + ":" + id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(id));
            }
        }

        public ObjectKey(params string[] keys)
            :base(string.Join(":", keys), FieldConstants.Object + ":" + string.Join(":", keys))
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            if (keys.Length == 0)
            {
                throw new ArgumentException("Value cannot be an empty collection.", nameof(keys));
            }
        }
    }
}