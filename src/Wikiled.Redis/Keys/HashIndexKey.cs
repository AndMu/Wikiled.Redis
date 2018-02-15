using Wikiled.Common.Arguments;
using Wikiled.Redis.Persistency;

namespace Wikiled.Redis.Keys
{
    public class HashIndexKey : IIndexKey
    {
        public HashIndexKey(string key, string hashKey)
        {
            Guard.NotNullOrEmpty(() => key, key);
            Guard.NotNullOrEmpty(() => hashKey, hashKey);
            Key = key;
            HashKey = hashKey;
        }

        public HashIndexKey(IRepository repository, string key, string hashKey)
        {
            Guard.NotNullOrEmpty(() => key, key);
            Guard.NotNullOrEmpty(() => hashKey, hashKey);
            Guard.NotNull(() => repository, repository);
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
