using Wikiled.Core.Utility.Arguments;
using Wikiled.Redis.Logic;

namespace Wikiled.Redis.Keys
{
    public class ObjectKey : BaseKey
    {
        public ObjectKey(string id)
            : base(id, FieldConstants.Object + ":" + id)
        {
            Guard.NotNullOrEmpty(() => id, id);
        }

        public ObjectKey(params string[] keys)
            :base(string.Join(":", keys), FieldConstants.Object + ":" + string.Join(":", keys))
        {
            Guard.NotNull(() => keys, keys);
            Guard.IsValid(() => keys, keys, item => item.Length > 0, "Pleace specify non empty array");
        }
    }
}