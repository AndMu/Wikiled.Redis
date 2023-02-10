using System;
using Wikiled.Redis.Persistency;

namespace Wikiled.Redis.Keys
{
    public class IndexKey : IIndexKey
    {
        public IndexKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(key));
            }

            Key = key;
            RepositoryKey = string.Empty;
        }

        public IndexKey(IRepository repository, string key)
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
        }

        public string RepositoryKey { get; }

        public string Key { get; }
    }
}
