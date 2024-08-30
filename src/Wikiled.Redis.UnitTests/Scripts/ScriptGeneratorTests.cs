using Wikiled.Redis.Scripts;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Wikiled.Redis.UnitTests.Scripts
{
    [TestFixture]
    public class ScriptGeneratorTests
    {
        [Test]
        public void Test()
        {
            ScriptGenerator generator = new ScriptGenerator();
            ClassicAssert.AreEqual(generator.GenerateInsertScript(true, 1), generator.GenerateInsertScript(true, 1));
        }
    }
}
