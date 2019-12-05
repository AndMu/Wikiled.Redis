using Microsoft.Extensions.Logging;
using System;

namespace Wikiled.Redis.Persistency
{
    public class EntitySubscriber : IEntitySubscriber
    {
        private readonly ILoggerFactory loggerFactory;

        public EntitySubscriber(ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public IObservable<T> Subscribe<T>(IBasicRepository<T> repository)
            where T : class, new()
        {
            return new EntitySubscription<T>(loggerFactory.CreateLogger<EntitySubscription<T>>(), repository).CreateSubscription();
        }
    }
}
