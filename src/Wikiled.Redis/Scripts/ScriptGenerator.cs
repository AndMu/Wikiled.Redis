using System.Collections.Generic;
using System.Text;
using Wikiled.Core.Utility.Extensions;

namespace Wikiled.Redis.Scripts
{
    public class ScriptGenerator : IScriptGenerator
    {
        public string GenerateInsertScript(bool trim, int addRecords)
        {
            StringBuilder scriptBuilder = new StringBuilder();
            var arguments = GetArgumentList(addRecords).AccumulateItems(",");
            var lengthArgument = $"ARGV[{addRecords + 1}]";
            scriptBuilder.AppendLine($"redis.call('RPUSH', KEYS[1], {arguments})");
            if (trim)
            {
                scriptBuilder.AppendLine("local list_size = redis.call('LLEN', KEYS[1])");
                scriptBuilder.AppendLine($"if (list_size > tonumber({lengthArgument})) then");
                scriptBuilder.AppendLine($"redis.call('LTRIM', KEYS[1], 1, {lengthArgument})");
                scriptBuilder.AppendLine("end");
            }

            return scriptBuilder.ToString();
        }

        private static IEnumerable<string> GetArgumentList(int total)
        {
            for (int i = 0; i < total; i++)
            {
                yield return $"ARGV[{i + 1}]";
            }
        }
    }
}
