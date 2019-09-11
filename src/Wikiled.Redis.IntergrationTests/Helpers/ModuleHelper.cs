using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
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
            new RedisModule(new NullLogger<RedisModule>(), config).ConfigureServices(service);
            new LoggingModule().ConfigureServices(service);
            new CommonModule().ConfigureServices(service);
            Provider = service.BuildServiceProvider();
        }

        public ServiceProvider Provider { get; }
    }
}
