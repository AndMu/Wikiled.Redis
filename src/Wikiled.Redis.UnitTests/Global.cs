using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IO;
using NUnit.Framework;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.UnitTests
{
    [SetUpFixture]
    public class Global
    {
        public static HandlingDefinitionFactory HandlingDefinitionFactory { get; private set; } 

        public static RecyclableMemoryStreamManager Stream { get; private set; } = new RecyclableMemoryStreamManager();

        [OneTimeSetUp]
        public void Setup()
        {
            HandlingDefinitionFactory = new HandlingDefinitionFactory(new NullLogger<HandlingDefinitionFactory>(), Stream);
        }

        [OneTimeTearDown]
        public void Clean()
        {
        }
    }
}
