using NUnit.Framework;
using NUnit.Framework.Legacy;
using StackExchange.Redis;
using Wikiled.Redis.Channels;
using Wikiled.Redis.Data;

namespace Wikiled.Redis.UnitTests.Data
{
    [TestFixture]
    public class RedisValueExtractorTests
    {
        [Test]
        public void Construct()
        {
            ClassicAssert.IsTrue(RedisValueExtractor.IsPrimitive<int>());
            ClassicAssert.IsTrue(RedisValueExtractor.IsPrimitive<int?>());
            ClassicAssert.IsTrue(RedisValueExtractor.IsPrimitive<bool>());
            ClassicAssert.IsTrue(RedisValueExtractor.IsPrimitive<byte[]>());
            ClassicAssert.IsTrue(RedisValueExtractor.IsPrimitive<string>());
            ClassicAssert.IsFalse(RedisValueExtractor.IsPrimitive<Identity>());
        }

        [Test]
        public void SafeConvert()
        {
            var result = RedisValueExtractor.SafeConvert<int>(1);
            ClassicAssert.AreEqual(1, result);
            var resultString = RedisValueExtractor.SafeConvert<string>("1");
            ClassicAssert.AreEqual("1", resultString);
            var resultBool = RedisValueExtractor.SafeConvert<bool>(true);
            ClassicAssert.AreEqual(true, resultBool);
        }

        [Test]
        public void TryParsePrimitiveComplex()
        {
            var success = RedisValueExtractor.TryParsePrimitive<int?>(1, out RedisValue result);
            ClassicAssert.AreEqual(true, success);
            ClassicAssert.AreEqual((RedisValue)1, result);

            success = RedisValueExtractor.TryParsePrimitive<int?>(null, out result);
            ClassicAssert.AreEqual(true, success);
            ClassicAssert.AreEqual(RedisValue.Null, result);
        }

        [TestCase(1, true, "1")]
        [TestCase((long)1, true, "1")]
        [TestCase("1", true, "1")]
        [TestCase(true, true, "True")]
        [TestCase(null, false, null)]
        public void TryParsePrimitive(object instance, bool isSuccess, object value)
        {
            var success = RedisValueExtractor.TryParsePrimitive(instance, out RedisValue result);
            ClassicAssert.AreEqual(isSuccess, success);
            ClassicAssert.AreEqual((RedisValue)(string)value, result);
        }
    }
}
