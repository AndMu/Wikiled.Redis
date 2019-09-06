using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Wikiled.Common.Logging;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Indexing
{
    public static class IndexManagerExtension
    {
        private static readonly ILogger log = ApplicationLogging.CreateLogger("IndexManagerExtension");

        public static async Task Reindex(this IRedisLink link, IDataKey key)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            log.LogDebug("Redindex {0}", key);
            var manager = new IndexManagerFactory(link);
            var indexManagers = manager.Create(link.Database, key.Indexes);
            var tasks = new List<Task>();

            tasks.Add(indexManagers.Reset());

            await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false);

            var actualKey = (string)link.GetKey(key);
            var mask = Regex.Replace(actualKey, $"{FieldConstants.Object}:.*", $"{FieldConstants.Object}*", RegexOptions.IgnoreCase);
            var total = 0;

            tasks.Clear();
            foreach (var redisKey in link.Multiplexer.GetKeys(mask))
            {
                total++;
                var rawId = Regex.Replace(redisKey, $".*:{FieldConstants.Object}:", string.Empty, RegexOptions.IgnoreCase);
                tasks.Add(indexManagers.AddRawIndex(rawId));
            }

            log.LogDebug("Redindexed {0} {1}", key, total);
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
