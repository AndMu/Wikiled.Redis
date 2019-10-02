using Wikiled.Redis.Channels;

namespace Wikiled.Redis.IntegrationTests.MockData
{
    public class SimpleItem
    {
        public string Name { get; set; }

        public int Id { get; set; }

        public ChannelState State { get; set; }
    }
}
