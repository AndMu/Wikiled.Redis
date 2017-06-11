using System.Collections.Generic;
using Wikiled.Core.Utility.Arguments;

namespace Wikiled.Redis.Keys
{
    public abstract class BaseKey : IDataKey
    {
        private readonly List<IIndexKey> indexes = new List<IIndexKey>();

        protected BaseKey(string recordId, string fullKey)
        {
            Guard.NotNullOrEmpty(() => fullKey, fullKey);
            Guard.NotNullOrEmpty(() => recordId, recordId);
            RecordId = recordId;
            FullKey = fullKey;
        }

        public string FullKey { get; }

        public string RecordId { get; }

        public IIndexKey[] Indexes => indexes.ToArray();

        public virtual void AddIndex(IIndexKey key)
        {
            Guard.NotNull(() => key, key);
            indexes.Add(key);
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