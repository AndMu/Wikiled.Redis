using NLog.Extensions.Logging;
using NUnit.Framework;
using Wikiled.Common.Logging;

namespace Wikiled.Redis.IntegrationTests
{
    [SetUpFixture]
    public class Global
    {
        [OneTimeSetUp]
        public void Setup()
        {
            ApplicationLogging.LoggerFactory.AddNLog();
        }

        [OneTimeTearDown]
        public void Clean()
        {
        }
    }
}
