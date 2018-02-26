using System;
using System.Collections.Generic;
using System.Linq;

namespace AmiBroker.Plugin.Providers.Stooq
{
    internal class EodFileLoader : RemoteFileLoader
    {
        private const string EodUrl = "https://stooq.pl/q/d/l/?s={0}&d1={1}&d2={2}&i=d";

        public EodFileLoader(Configuration configuration) : base(configuration)
        {
        }

        internal override IEnumerable<string> Load()
        {
            if (!IsRefreshNeeded()) return Enumerable.Empty<string>();
            
            var startDate = Configuration.LastEntryInFile;
            var endDate = CalculateEndDate();
            var url = string.Format(EodUrl, Configuration.Ticker, startDate, endDate);

            return GetUrlReponse(url).ToList();
        }
        
        private bool IsRefreshNeeded()
        {
            var previousWorkingDate = CalculateEndDate();
            return !previousWorkingDate.Equals(Configuration.LastEntryInFile);
        }

        private static string CalculateEndDate()
        {
            var previousWorkingDate = DateTime.Now.AddDays(-1);
            while(previousWorkingDate.IsWeekend())
            {
                previousWorkingDate = previousWorkingDate.AddDays(-1);
            }

            return previousWorkingDate.ToString("yyyyMMdd");
        }
    }
}