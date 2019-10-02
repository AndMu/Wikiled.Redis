using System;
using System.Collections.Generic;
using System.Linq;

namespace Wikiled.Redis.Keys
{
    public abstract class BaseKey : IDataKey
    {
        private readonly Dictionary<string, IIndexKey> indexes = new Dictionary<string, IIndexKey>();

        protected BaseKey(string recordId, string fullKey)
        {
            if (string.IsNullOrEmpty(recordId))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(recordId));
            }

            if (string.IsNullOrEmpty(fullKey))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(fullKey));
            }

            RecordId = recordId;
            FullKey = fullKey;
        }

        public string FullKey { get; }

        public string RecordId { get; }

        public IIndexKey[] Indexes => indexes.Values.ToArray();

        public virtual void AddIndex(IIndexKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            indexes[key.Key] = key;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == GetType() && Equals((BaseKey)obj);
        }

        public override int GetHashCode()
        {
            return FullKey.GetHashCode();
        }

        /// <summary>
        ///     Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        ///     A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return GetType().Name + ":" + FullKey;
        }

        private bool Equals(BaseKey other)
        {
            return FullKey.Equals(other.FullKey);
        }
    }
}