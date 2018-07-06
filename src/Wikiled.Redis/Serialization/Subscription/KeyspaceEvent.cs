using System;
using StackExchange.Redis;

namespace Wikiled.Redis.Serialization.Subscription
{
    public class KeyspaceEvent
    {
        public KeyspaceEvent(string key, RedisChannel channel, RedisValue value)
        {
            if (channel.IsNullOrEmpty)
            {
                throw new ArgumentException(nameof(channel));
            }

            if (value.IsNullOrEmpty)
            {
                throw new ArgumentException(nameof(value));
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(key));
            }

            Channel = channel;
            Key = key;
            Value = value;
        }

        public RedisChannel Channel { get; }

        public string Key { get; }

        public RedisValue Value { get; }
    }
}
