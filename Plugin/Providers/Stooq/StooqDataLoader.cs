using System.Collections.Generic;
using System.Linq;

namespace AmiBroker.Plugin.Providers.Stooq
{
    internal class StooqDataLoader
    {
        private readonly FileLoader _localFileLoader;
        private readonly FileLoader _eodFileLoader;
        private readonly FileLoader _intradayFileLoader;

        internal StooqDataLoader(FileLoader localFileLoader, FileLoader eodFileLoader, FileLoader intradayFileLoader)
        {
            _localFileLoader = localFileLoader;
            _eodFileLoader = eodFileLoader;
            _intradayFileLoader = intradayFileLoader;
        }

        internal IEnumerable<string> LoadFile()
        {
            var result = _localFileLoader.Load().ToList();
            var fileLength = result.Count;
            
            result = Merge(result, _eodFileLoader.Load()).ToList();
            if (fileLength < result.Count)
            {
                _localFileLoader.SaveFile(result);
            }
            result.Add("--- INTRADAY ---");
            result.AddRange(_intradayFileLoader.Load());

            return result.ToList();
        }
        
        private static IEnumerable<string> Merge(IEnumerable<string> first, IEnumerable<string> second)
        {
            // first line of the stooq file has to be skipped
            if (second.Count() < 2) return first;
            if (first.Count() < 2) return second;

            var validSecondLines = second.Where(ContentValidator.IsValid);

            if (!validSecondLines.Any()) return first;

            var firstDateInSeconod = validSecondLines.First().AsInt();

            // find the last entry in first list which is older then the first entry in the second list
            var result = first
                .Where(x => !string.IsNullOrEmpty(x))
                .TakeWhile(x => x.AsInt() < firstDateInSeconod).ToList();
            result.AddRange(validSecondLines);

            return result; 
        }
    }
}