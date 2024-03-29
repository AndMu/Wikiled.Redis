﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wikiled.Common.Logging;
using Wikiled.Redis.Keys;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Indexing
{
    public static class IndexManagerExtension
    {
        public static async Task Reindex(this IRedisLink link, ILoggerFactory factory, IDataKey key)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var log = factory.CreateLogger("IndexManagerExtension");

            log.LogDebug("Redindex {0}", key);
            var manager = new IndexManagerFactory(factory, link);
            var tasks = new List<Task>();
            var total = 0;
            foreach (var index in key.Indexes)
            {
                var indexManagers = manager.Create(index);

                tasks.Add(indexManagers.Reset(link.Database, index));

                await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false);

                var actualKey = (string)link.GetKey(key);
                var mask = Regex.Replace(actualKey, $"{FieldConstants.Object}:.*", $"{FieldConstants.Object}*", RegexOptions.IgnoreCase);

                tasks.Clear();
                foreach (var redisKey in link.Multiplexer.GetKeys(mask))
                {
                    total++;
                    var rawId = Regex.Replace(redisKey, $".*:{FieldConstants.Object}:", string.Empty, RegexOptions.IgnoreCase);
                    tasks.Add(indexManagers.AddRawIndex(link.Database, rawId, index));
                }
            }
            
            await Task.WhenAll(tasks).ConfigureAwait(false);
            log.LogDebug("ReIndexed {0} {1}", key, total);
        }
    }
}
