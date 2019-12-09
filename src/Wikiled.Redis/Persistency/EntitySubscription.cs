using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Persistency
{
    public class EntitySubscription<T> : IEntitySubscription<T>
        where T : class, new()
    {
        private readonly IBasicRepository<T> repository;

        private readonly ILogger<EntitySubscription<T>> logger;

        public EntitySubscription(ILogger<EntitySubscription<T>> logger, IBasicRepository<T> repository)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public IObservable<(IDataKey Key, string Command, T Instance)> CreateSubscription()
        {
            var key = repository.Entity.GetKey("*");

            return Observable.Create<ChannelMessage>(
                                 observer =>
                                 {
                                     var diposable = new CancellationDisposable();
                                     Task.Run(() => EntityEventSubscription(key, diposable.Token, observer));
                                     return diposable;
                                 })
                             .Select(Convert)
                             .Merge()
                             .Where(item => item.Key != null)
                             .Publish()
                             .RefCount();
        }

        private async Task EntityEventSubscription(IDataKey key, CancellationToken token, IObserver<ChannelMessage> observer)
        {
            ChannelMessageQueue subscriber = null;
            try
            {
                subscriber = await repository.Redis.Multiplexer.SubscribeKeyEvents(key.FullKey).ConfigureAwait(false);

                do
                {
                    var result = await subscriber.ReadAsync(token).ConfigureAwait(false);
                    observer.OnNext(result);
                    while (subscriber.TryRead(out result))
                    {
                        observer.OnNext(result);
                    }
                } while (!token.IsCancellationRequested);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (subscriber != null)
                {
                    await subscriber.UnsubscribeAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task<(IDataKey Key, string Command, T Instance)> Convert(ChannelMessage message)
        {
            if (string.Compare(message.Message, "expire", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return (null, message.Message, null);
            }

            var receivedKey = message.Channel.ToString();
            var start = receivedKey.IndexOf(FieldConstants.ObjectTag) + 8;
            if (start < 0)
            {
                logger.LogWarning("Bad key: {0}", receivedKey);
                return (null, message.Message, null);
            }

            receivedKey = receivedKey.Substring(start);
            var keyItem = new RepositoryKey(repository, new ObjectKey(receivedKey));
            var instance = await repository.Redis.Client.GetRecords<T>(keyItem).LastOrDefaultAsync();

            return (keyItem, message.Message, instance);
        }
    }
}
