using System;
using System.Collections.Generic;
using Wikiled.Common.Arguments;

namespace Wikiled.Redis.Information
{
    public abstract class BaseInformation
    {
        private readonly string category;

        protected BaseInformation(IServerInformation main, string category)
        {
            Guard.NotNull(() => main, main);
            Guard.NotNullOrEmpty(() => category, category);
            Main = main;
            this.category = category;
        }

        public IServerInformation Main { get; }

        protected string GetType(string type)
        {
            if (!Main.RawData.TryGetValue(category, out Dictionary<string, string> types) ||
                !types.TryGetValue(type, out string value))
            {
                return null;
            }

            return value;
        }

        protected T? GetType<T>(string type)
            where T : struct
        {
            var value = GetType(type);
            if (value == null)
            {
                return null;
            }

            var typeDefinition = typeof(T);

            if (typeDefinition.IsEnum)
            {
                return (T)Enum.Parse(typeDefinition, value, true);
            }

            return (T)Convert.ChangeType(value, typeDefinition);
        }
    }
}
