using System;
using Wikiled.Redis.Persistency;

namespace Wikiled.Redis.Keys
{
    public class RepositoryKey : BaseKey
    {
        public RepositoryKey(IRepository repository, ObjectKey key)
            : base(key.RecordId, $"{repository.Name}:{key.FullKey}")
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            Repository = repository;
        }

        public IRepository Repository { get; }

        public override void AddIndex(IIndexKey key)
        {
            if (key?.RepositoryKey == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            base.AddIndex(key);
        }
    }
}