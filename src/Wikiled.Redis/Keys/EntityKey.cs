using System;
using Wikiled.Redis.Persistency;

namespace Wikiled.Redis.Keys
{
    public class EntityKey
    {
        private readonly IRepository repository;

        public EntityKey(string entityName, IRepository repository)
        {
            EntityName = entityName ?? throw new ArgumentNullException(nameof(entityName));
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            AllIndex = new IndexKey(repository, AllEntitiesTag, true);
        }

        public string EntityName { get; }

        public IndexKey AllIndex { get; }

        protected string AllEntitiesTag => $"All.{EntityName}s";

        public IDataKey GetKey(string id)
        {
            string[] key = { EntityName, id.ToLower() };
            return new RepositoryKey(repository, new ObjectKey(key));
        }
    }
}
