using Wikiled.Common.Arguments;
using Wikiled.Redis.Persistency;

namespace Wikiled.Redis.Keys
{
    public class IndexKey : IIndexKey
    {
        public IndexKey(string key, bool isSet)
        {
            Guard.NotNullOrEmpty(() => key, key);
            Key = key;
            IsSet = isSet;
        }

        public IndexKey(IRepository repository, string key, bool isSet)
        {
            Guard.NotNullOrEmpty(() => key, key);
            Guard.NotNull(() => repository, repository);
            Key = key;
            RepositoryKey = repository.Name;
            IsSet = isSet;
        }

        public bool IsSet { get; }

        public string RepositoryKey { get; }

        public string Key { get; }
    }
}
