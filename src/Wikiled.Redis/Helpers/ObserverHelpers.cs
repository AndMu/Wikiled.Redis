using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Helpers
{
    public static class ObserverHelpers
    {
        public static IObservable<T> InnerJoin<T>(this IObservable<T> first, IObservable<T> second)
            where T : IEquatable<T>
        {
            return first.Join(
                            second,
                            _ => Observable.Never<Unit>(),
                            _ => Observable.Never<Unit>(),
                            (aOutput, bOutput) => (aOutput, bOutput))
                        .Where(tupple => tupple.Item1.Equals(tupple.Item2))
                        .Select(item => item.Item1);
        }

        public static IObservable<HashEntry> GetHash(this IRedisLink link, string key)
        {
            return Observable.Create<HashEntry>(
                obs =>
                {
                    IEnumerable<HashEntry> hashEntries = link.Database.HashScan(key);
                    foreach (var entry in hashEntries)
                    {
                        obs.OnNext(entry);
                    }

                    obs.OnCompleted();
                    return Disposable.Empty;
                });
        }

        public static IObservable<T> Batch<T>(
            Task<long> count,
            Func<long, long, IObservable<IDataKey>> getKeys,
            Func<IDataKey, IObservable<T>> getData,
            int batchSize,
            long start,
            long end = -1)
        {
            var indexes = Observable.Create<long>(
                async observer =>
                {
                    var lastIndex = await count.ConfigureAwait(false) - 1;
                    lastIndex = end == -1 || end > lastIndex ? lastIndex : end;
                    for (long i = start; i <= lastIndex; i++)
                    {
                        observer.OnNext(i);
                    }

                    observer.OnCompleted();
                });

            return indexes.Buffer(batchSize)
                          .Select(keys => Create(keys, getKeys, getData))
                          .Concat();
        }

        private static IObservable<T> Create<T>(
            IList<long> keys, 
            Func<long, long, IObservable<IDataKey>> getKeys,
            Func<IDataKey, IObservable<T>> getData)
        {
            return Observable.Create<T>(
                async observer =>
                {
                    var itemKeys = await getKeys(keys[0], keys[keys.Count - 1]).ToArray();
                    foreach (var itemKey in itemKeys)
                    {
                        var result = await getData(itemKey).ToArray();
                        foreach (var record in result)
                        {
                            observer.OnNext(record);
                        }
                    }

                    observer.OnCompleted();
                });
        }
    }
}
