using Microsoft.Extensions.Logging;
using System;
using Wikiled.Redis.Keys;

namespace Wikiled.Redis.Persistency
{
    public class EntitySubscriber : IEntitySubscriber
    {
        private readonly ILoggerFactory loggerFactory;

        public EntitySubscriber(ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public IObservable<(IDataKey Key, string Command, T Intance)> Subscribe<T>(IBasicRepository<T> repository)
            where T : class, new()
        {
            return new EntitySubscription<T>(loggerFactory.CreateLogger<EntitySubscription<T>>(), repository).CreateSubscription();
        }
    }
}
