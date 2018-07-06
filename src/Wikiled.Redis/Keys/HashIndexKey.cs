using System;
using Wikiled.Redis.Persistency;

namespace Wikiled.Redis.Keys
{
    public class HashIndexKey : IIndexKey
    {
        public HashIndexKey(string key, string hashKey)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(key));
            }

            if (string.IsNullOrEmpty(hashKey))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(hashKey));
            }

            Key = key;
            HashKey = hashKey;
        }

        public HashIndexKey(IRepository repository, string key, string hashKey)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(key));
            }

            if (string.IsNullOrEmpty(hashKey))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(hashKey));
            }

            Key = key;
            HashKey = hashKey;
            RepositoryKey = repository.Name;
        }

        public bool IsSet => false;

        public string RepositoryKey { get; }

        public string Key { get; }

        public string HashKey { get; }

        
    }
}
