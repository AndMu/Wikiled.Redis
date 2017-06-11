namespace Wikiled.Common.Persistency
{
    public class CachingStrategy
    {
        private static readonly CachingStrategy nullInstance = new CachingStrategy();

        public CachingStrategy(bool isNew = false, bool disabled = false)
        {
            IsNew = isNew;
            DisableCaching = disabled;
        }

        public static CachingStrategy Null
        {
            get
            {
                return nullInstance;
            }
        }

        public bool DisableCaching { get; private set; }

        /// <summary>
        ///     Do we care only about latest versions caching
        /// </summary>
        public bool IsNew { get; private set; }
    }
}