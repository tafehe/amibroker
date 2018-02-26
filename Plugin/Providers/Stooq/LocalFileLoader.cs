using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AmiBroker.Plugin.Providers.Stooq
{
    internal class LocalFileLoader : FileLoader
    {
        public LocalFileLoader(Configuration configuration) : base(configuration)
        {
        }

        internal override IEnumerable<string> Load()
        {
            if (!File.Exists(Configuration.TickerFile)) return Enumerable.Empty<string>();
            
            var fileContent = File.ReadAllLines(Configuration.TickerFile).ToList();

            return fileContent;
        }
    }
}