using System.Linq;
using NUnit.Framework;
using System.Reactive.Linq;
using NUnit.Framework.Legacy;
using Wikiled.Redis.Helpers;

namespace Wikiled.Redis.UnitTests.Helpers
{
    [TestFixture]
    public class ObserverHelpersTests
    {
        [Test]
        public void InnerJoin()
        {
            var a = Observable.Range(1, 10);
            var b = Observable.Range(5, 10);
            var c = Observable.Range(3, 4).ToEnumerable().ToArray().Reverse().ToObservable();
            var joinedStream = a.InnerJoin(b).InnerJoin(c);
            var result = joinedStream.ToEnumerable().ToArray();
            ClassicAssert.AreEqual(2, result.Length);
            ClassicAssert.AreEqual(5, result[0]);
            ClassicAssert.AreEqual(6, result[1]);
        }
    }
}
