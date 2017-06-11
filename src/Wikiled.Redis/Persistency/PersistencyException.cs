using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Wikiled.Redis.Persistency
{
    [Serializable]
    [ExcludeFromCodeCoverage]
    public class PersistencyException : Exception
    {
        public PersistencyException()
        {
        }

        public PersistencyException(string message)
            : base(message)
        {
        }

        public PersistencyException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected PersistencyException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
