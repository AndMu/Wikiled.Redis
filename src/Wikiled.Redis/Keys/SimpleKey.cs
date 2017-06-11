using Wikiled.Core.Utility.Arguments;

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
            Guard.NotNull(() => objectKey, objectKey);
            Guard.NotNullOrEmpty(() => repository, repository);
            ObjectKey = objectKey;
        }

        public ObjectKey ObjectKey { get; }

        public static SimpleKey GenerateKey(string repository, string objectName)
        {
            Guard.NotNullOrEmpty(() => repository, repository);
            Guard.NotNullOrEmpty(() => objectName, objectName);
            ObjectKey objectKey = new ObjectKey(objectName);
            return new SimpleKey(repository, objectKey);
        }
    }
}