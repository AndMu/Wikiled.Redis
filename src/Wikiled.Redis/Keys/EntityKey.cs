using System;
using Wikiled.Redis.Persistency;

namespace Wikiled.Redis.Keys
{
    public class EntityKey
    {
        private readonly IRepository repository;

        private const string allEntitiesTag = "All";

        public EntityKey(string entityName, IRepository repository)
        {
            EntityName = entityName ?? throw new ArgumentNullException(nameof(entityName));
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            AllIndex = new IndexKey(repository, allEntitiesTag, true);
        }

        public string EntityName { get; }

        public IndexKey AllIndex { get; }

        public IDataKey GetKey(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id), "Id can't be null'");
            }

            string[] key = { EntityName, id.ToLower() };
            return new RepositoryKey(repository, new ObjectKey(key));
        }

        public IIndexKey GenerateIndex(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name), "Index name can't be null'");
            }

            return new IndexKey(repository, name, true);
        }
    }
}
