using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AmiBroker.Plugin.Providers.Stooq
{
    internal static class ContentValidator
    {
        private static readonly Regex LinePatter = new Regex(@"^[0-9,\.,-]+$");
        
        internal static string GetLastDate(IEnumerable<string> fileContent)
        {
            return fileContent.Where(x => !string.IsNullOrEmpty(x))
                .DefaultIfEmpty(string.Empty)
                .Select(x => x.Split(',')[0])
                .Last();
        }

        internal static bool IsValid(string line)
        {
            return !string.IsNullOrEmpty(line) && LinePatter.IsMatch(line);
        }
    }
}