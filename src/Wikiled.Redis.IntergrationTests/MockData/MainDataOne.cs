using System;
using ProtoBuf;

namespace Wikiled.Redis.IntegrationTests.MockData
{
    [ProtoContract]
    [Serializable]
    public class MainDataOne : IMainData
    {
        [ProtoMember(1)]
        public string Name { get; set; }
    }
}
