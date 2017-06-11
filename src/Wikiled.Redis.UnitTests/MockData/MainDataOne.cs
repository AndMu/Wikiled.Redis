using System;
using ProtoBuf;

namespace Wikiled.Redis.UnitTests.MockData
{
    [ProtoContract]
    [Serializable]
    public class MainDataOne : IMainData
    {
        [ProtoMember(1)]
        public string Name { get; set; }
    }
}
