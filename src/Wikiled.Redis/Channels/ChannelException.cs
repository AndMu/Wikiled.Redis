using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Wikiled.Redis.Channels
{
    [Serializable]
    [ExcludeFromCodeCoverage]
    public class ChannelException : Exception
    {
        public ChannelException()
        {
        }

        public ChannelException(string message)
            : base(message)
        {
        }

        public ChannelException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected ChannelException(
            SerializationInfo info, 
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}