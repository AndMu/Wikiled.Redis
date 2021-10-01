using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using NUnit.Framework;
using Wikiled.Common.Logging;

namespace Wikiled.Redis.IntegrationTests
{
    [SetUpFixture]
    public class Global
    {
        public static ServiceProvider Services { get; private set; }

        public static ILogger Logger { get; private set; }

        public static ILoggerFactory LoggerFactory { get; private set; }

        [OneTimeSetUp]
        public void Setup()
        {
            var collection = new ServiceCollection();
            collection.AddLogging(
                builder =>
                {
                    builder.SetMinimumLevel(LogLevel.Trace);
                    builder.AddNLog();
                });

            Services = collection.BuildServiceProvider();
            LoggerFactory = Services.GetRequiredService<ILoggerFactory>();
            Logger = LoggerFactory.CreateLogger<Global>();
        }

        [OneTimeTearDown]
        public void Clean()
        {
        }
    }
}
