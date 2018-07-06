using System;
using Wikiled.Redis.Persistency;

namespace Wikiled.Redis.Keys
{
    public class IndexKey : IIndexKey
    {
        public IndexKey(string key, bool isSet)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(key));
            }

            Key = key;
            IsSet = isSet;
        }

        public IndexKey(IRepository repository, string key, bool isSet)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(key));
            }

            Key = key;
            RepositoryKey = repository.Name;
            IsSet = isSet;
        }

        public bool IsSet { get; }

        public string RepositoryKey { get; }

        public string Key { get; }
    }
}
