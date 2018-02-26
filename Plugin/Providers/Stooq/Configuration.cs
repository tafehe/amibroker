using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AmiBroker.Plugin.Providers.Stooq
{
    internal class Configuration
    {
        private const string DefaultStartDate = "1970-01-01 00:00";
        
        internal string LastDownloadRun
        {
            get => _config.GetValue("LAST_DOWNLOAD_RUN", DefaultStartDate);
            set => _config.AddOrReplaceValue("LAST_DOWNLOAD_RUN", value);
        }

        internal string LastEntryInFile
        {
            get => _config.GetValue("LAST_ENTRY_IN_FILE", "19700101").Replace("-", "");
            set => _config.AddOrReplaceValue("LAST_ENTRY_IN_FILE", value);
        }

        internal string Ticker
        {
            get => _config["TICKER"];
            private set => _config.AddOrReplaceValue("TICKER", value);
        }

        internal string TickerFile
        {
            get => _config["TICKER_FILE"];
            private set => _config.AddOrReplaceValue("TICKER_FILE", value);
        }

        private string TickerConfigFile
        {
            get => _config["TICKER_CONFIG_FILE"];
            set => _config.AddOrReplaceValue("TICKER_CONFIG_FILE", value);
        }
        
        private readonly IDictionary<string, string> _config;

        public Configuration(string databasePath, string ticker)
        {
            var file = databasePath + @"\" + ticker + ".csv";
            var configFile = databasePath + @"\" + ticker + ".config";

            _config = LoadConfig(configFile);
            Ticker = ticker;
            TickerFile = file;
            TickerConfigFile = configFile;
        }
        
        private static IDictionary<string, string> LoadConfig(string configFile)
        {
            if (!File.Exists(configFile)) return new Dictionary<string, string>();

            return File.ReadAllLines(configFile).ToDictionary(
                line => line.Split('=')[0],
                line => line.Split('=')[1]
            );
        }

        internal void Save()
        {
            var configLines = _config
                .Where(dictEntry => !"TICKER".Equals(dictEntry.Key) 
                                 && !"TICKER_FILE".Equals(dictEntry.Key) 
                                 && !"TICKER_CONFIG_FILE".Equals(dictEntry.Key))
                .Select(keyValue => string.Join("=", keyValue.Key, keyValue.Value))
                .ToList();
            File.WriteAllLines(TickerConfigFile, configLines);
        }
    }
}