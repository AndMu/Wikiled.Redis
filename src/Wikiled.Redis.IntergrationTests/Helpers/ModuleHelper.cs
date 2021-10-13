using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wikiled.Common.Utilities.Modules;
using Wikiled.Redis.Config;
using Wikiled.Redis.Modules;

namespace Wikiled.Redis.IntegrationTests.Helpers
{
    public class ModuleHelper
    {
        public ModuleHelper(RedisConfiguration config)
        {
            var service = new ServiceCollection();
            service.AddLogging(builder => builder.AddDebug());
            service.RegisterModule(new RedisModule(config));
            service.RegisterModule<CommonModule>();
            Provider = service.BuildServiceProvider();
        }

        public ServiceProvider Provider { get; }
    }
}
