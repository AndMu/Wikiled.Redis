namespace Wikiled.Redis.Persistency
{
    public class CachingStrategy
    {
        public CachingStrategy(bool isNew = false, bool disabled = false)
        {
            IsNew = isNew;
            DisableCaching = disabled;
        }

        public static CachingStrategy Null { get; } = new CachingStrategy();

        public bool DisableCaching { get; }

        /// <summary>
        ///     Do we care only about latest versions caching
        /// </summary>
        public bool IsNew { get; }
    }
}