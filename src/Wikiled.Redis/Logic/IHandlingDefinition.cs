using Wikiled.Redis.Data;

namespace Wikiled.Redis.Logic
{
    public interface IHandlingDefinition
    {
        IDataSerializer DataSerializer { get; }

        bool ExtractType { get; set; }

        bool IsSet { get; set; }

        /// <summary>
        ///     Is given type persisted as single type
        /// </summary>
        bool IsSingleInstance { get; set; }

        /// <summary>
        ///     Is well known
        /// </summary>
        bool IsWellKnown { get; }

        /// <summary>
        ///     Get Next id for the given type
        /// </summary>
        /// <returns></returns>
        string GetNextId();
    }
}