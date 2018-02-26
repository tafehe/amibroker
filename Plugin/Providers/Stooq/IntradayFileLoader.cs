using System;
using System.Collections.Generic;
using System.Linq;

namespace AmiBroker.Plugin.Providers.Stooq
{
    internal class IntradayFileLoader : RemoteFileLoader
    {
        private const string IntradyUrl = "https://stooq.pl/q/l/?s={0}&f=d1ohlcv";
        
        public IntradayFileLoader(Configuration configuration) : base(configuration)
        {
        }
        
        internal override IEnumerable<string> Load()
        {
            if (DateTime.Now.IsWeekend()) return Enumerable.Empty<string>();

            var url = string.Format(IntradyUrl, Configuration.Ticker);
            var fileContent = GetUrlReponse(url);

            return fileContent.ToList();
        }
    }
}