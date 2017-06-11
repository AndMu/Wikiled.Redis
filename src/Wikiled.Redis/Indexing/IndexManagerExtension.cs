using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NLog;
using StackExchange.Redis;
using Wikiled.Core.Utility.Arguments;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Indexing
{
    public static class IndexManagerExtension
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public static async Task Reindex(this IRedisLink link, IDataKey key)
        {
            Guard.NotNull(() => link, link);
            Guard.NotNull(() => key, key);

            log.Debug("Redindex {0}", key);
            IndexManagerFactory manager = new IndexManagerFactory(link, link.Database);
            var indexManagers = key.Indexes.Select(manager.Create).ToArray();
            if (indexManagers.Length != 0)
            {
                List<Task> tasks = new List<Task>();
                foreach (var indexManager in indexManagers)
                {
                    tasks.Add(indexManager.Reset());
                }

                await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false);
                
                var actualKey = (string)link.GetKey(key);
                var mask = Regex.Replace(actualKey, $"{FieldConstants.Object}:.*", $"{FieldConstants.Object}*", RegexOptions.IgnoreCase);
                int total = 0;

                tasks.Clear();
                foreach (RedisKey redisKey in link.Multiplexer.GetKeys(mask))
                {
                    total++;
                    var rawId = Regex.Replace(redisKey, $".*:{FieldConstants.Object}:", string.Empty, RegexOptions.IgnoreCase);
                    foreach (var indexManager in indexManagers)
                    {
                        tasks.Add(indexManager.AddRawIndex(rawId));
                    }
                }

                log.Debug("Redindexed {0} {1}", key, total);
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }
    }
}
