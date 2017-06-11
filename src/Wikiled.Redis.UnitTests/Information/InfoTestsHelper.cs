using System.Collections.Generic;
using Wikiled.Redis.Information;
using Moq;

namespace Wikiled.Redis.UnitTests.Information
{
    public static class InfoTestsHelper
    {
        public static IServerInformation Create(string name, Dictionary<string, string> values)
        {
            Mock<IServerInformation> serverInfo = new Mock<IServerInformation>();
            var table = new Dictionary<string, Dictionary<string, string>>();
            table[name] = values;
            serverInfo.Setup(item => item.RawData).Returns(table);
            return serverInfo.Object;
        }
    }
}
