using Wikiled.Core.Utility.Arguments;
using Wikiled.Redis.Persistency;

namespace Wikiled.Redis.Keys
{
    public class RepositoryKey : BaseKey
    {
        public RepositoryKey(IRepository repository, ObjectKey key)
            : base(key.RecordId, $"{repository.Name}:{key.FullKey}")
        {
            Guard.NotNull(() => repository, repository);
            Guard.NotNull(() => key, key);
            Repository = repository;
        }

        public IRepository Repository { get; }

        public override void AddIndex(IIndexKey key)
        {
            Guard.NotNull(() => key, key);
            Guard.NotNullOrEmpty(() => key.RepositoryKey, key.RepositoryKey);
            base.AddIndex(key);
        }
    }
}