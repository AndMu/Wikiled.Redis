using System;

namespace Wikiled.Redis.Keys
{
    public class SimpleKey : BaseKey
    {
        public SimpleKey(params string[] keys)
            : base(keys[0], string.Join(":", keys))
        {
        }

        private SimpleKey(string repository, ObjectKey objectKey)
            : base(objectKey.RecordId, repository + ":" + objectKey.FullKey)
        {
            if (string.IsNullOrEmpty(repository))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(repository));
            }

            ObjectKey = objectKey;
        }

        public ObjectKey ObjectKey { get; }

        public static SimpleKey GenerateKey(string repository, string objectName)
        {
            if (string.IsNullOrEmpty(repository))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(repository));
            }

            if (string.IsNullOrEmpty(objectName))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(objectName));
            }

            var objectKey = new ObjectKey(objectName);
            return new SimpleKey(repository, objectKey);
        }
    }
}