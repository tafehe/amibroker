using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace AmiBroker.Plugin.Providers.Stooq
{
    internal class StooqDataLoader
    {
        private const string DefaultStartDate = "1970-01-01 00:00";
        private const string DownloadLastRun = "LAST_DOWNLOAD_RUN";
        private const string LastEntryInFile = "LAST_ENTRY_IN_FILE";
        private const string SecondTimeCheckAfter = "SECOND_CHECK_ON_OR_AFTER";
        private const string EodUrl =     "https://stooq.pl/q/d/l/?s={0}&d1={1}&d2={2}&i=d";
        private const string IntradyUrl = "https://stooq.pl/q/l/?s={0}&f=d1ohlcv";
        private readonly Regex _linePatter = new Regex(@"^[0-9,\.,-]+$");
        private readonly IDictionary<string, string> _config;
        private readonly string _configFile;
        private readonly string _file;
        private readonly string _ticker;

        internal StooqDataLoader(string ticker, string databasePath)
        {
            _ticker = ticker;
            _file = databasePath + @"\" + ticker + ".csv";
            _configFile = databasePath + @"\" + ticker + ".config";
            _config = LoadConfig();
        }

        public IEnumerable<string> LoadFile()
        {
            var result = LoadLocalFile();

            if (IsRefreshNeeded()) result = Merge(LoadRemoteEodFile, result);

            SaveFile(result);
            SaveConfig();

            result = Merge(LoadRemoteIntradayFile, result);

            return result;
        }

        private IEnumerable<string> Merge(Func<IEnumerable<string>> loadFunc, IEnumerable<string> source)
        {
            var toBeAppended = loadFunc();

            // first line of the stooq file has to be skipped
            if (source.Count() < 2) return toBeAppended;
            if (toBeAppended.Count() < 2) return source;

            var validatedLines = toBeAppended.Where(IsValid);

            if (!validatedLines.Any()) return source;

            var firstDateInLoaded = GetStartDateAsInt(validatedLines.First());

            // find the last entry in first list which is older then the first entry in the second list
            var validatedSource = source
                .Where(x => !string.IsNullOrEmpty(x))
                .TakeWhile(x => GetStartDateAsInt(x) < firstDateInLoaded).ToList();
            validatedSource.AddRange(validatedLines);

            return validatedSource;
        }

        private int GetStartDateAsInt(string line)
        {
            return IsValid(line) 
                ? Convert.ToInt32(line.Split(',')[0].Replace("-", ""))
                : string.IsNullOrEmpty(line) ? 99999999 : -1;
        }

        private IEnumerable<string> LoadRemoteEodFile()
        {
            var startDate = _config.GetValue(LastEntryInFile, "19700101").Replace("-", "");
            var endDate = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
            var url = string.Format(EodUrl, _ticker, startDate, endDate);
            var fileContent = GetUrlReponse(url);

            _config.AddOrReplaceValue(LastEntryInFile, GetLastValidLine(fileContent));
            
            return fileContent;
        } 

        private IEnumerable<string> LoadRemoteIntradayFile()
        {
            if (IsWeekend()) return Enumerable.Empty<string>();

            var url = string.Format(IntradyUrl, _ticker);
            var fileContent = GetUrlReponse(url);

            return fileContent;
        }

        private static bool IsWeekend()
        {
            return DateTime.Now.DayOfWeek == DayOfWeek.Saturday
                || DateTime.Now.DayOfWeek == DayOfWeek.Sunday;
        }

        private static IEnumerable<string> GetUrlReponse(string url)
        {
            var request = WebRequest.Create(url);
            var response = request.GetResponse();

            var dataStream = response.GetResponseStream();

            if (dataStream == null) return Enumerable.Empty<string>();
            
            // Open the stream using a StreamReader for easy access.
            var reader = new StreamReader(dataStream);
            // Read the content.
            var responseFromServer = reader.ReadToEnd();

            return Regex.Split(responseFromServer, @"\r\n").ToList();

        }

        private IEnumerable<string> LoadLocalFile()
        {
            if (!File.Exists(_file)) return Enumerable.Empty<string>();
            
            var fileContent = File.ReadAllLines(_file);
            var lastLine = GetLastValidLine(fileContent);
            _config.AddOrReplaceValue(LastEntryInFile, lastLine);

            return fileContent; 
        }

        private static string GetLastValidLine(IEnumerable<string> fileContent)
        {
            return fileContent.Where(x => !string.IsNullOrEmpty(x))
                .DefaultIfEmpty(string.Empty)
                .Select(x => x.Split(',')[0])
                .Last();
        }

        private bool IsValid(string line)
        {
            return !string.IsNullOrEmpty(line) && _linePatter.IsMatch(line);
        }

        private Boolean IsRefreshNeeded()
        {
            var downloadLastRun = _config.GetDateValue(DownloadLastRun, DefaultStartDate);

            if (DateTime.Now.Date > downloadLastRun.Date) return true;

            var secondCheckTime = _config.GetTimeValue(SecondTimeCheckAfter);

            // if last downloaded time was before second time download check
            // and now is after second time check then refresh is needed.
            return downloadLastRun.TimeOfDay <= secondCheckTime && secondCheckTime <= DateTime.Now.TimeOfDay;
        }

        private void SaveFile(IEnumerable<string> fileContent)
        {
            _config.AddOrReplaceValue(DownloadLastRun, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            File.WriteAllLines(_file, fileContent);
        }

        private IDictionary<string, string> LoadConfig()
        {
            if (!File.Exists(_configFile)) return new Dictionary<string, string>();

            return File.ReadAllLines(_configFile).ToDictionary(
                line => line.Split('=')[0],
                line => line.Split('=')[1]
            );
        }

        private void SaveConfig()
        {
            var configLines = _config.Select(keyValue => keyValue.Key + "=" + keyValue.Value).ToList();
            File.WriteAllLines(_configFile, configLines);
        }
    }

    public static class LocalExtentions
    {
        public static string GetValue(this IDictionary<string, string> dictionary, string key, string defaultValue)
        {
            return !dictionary.TryGetValue(key, out string value) ? defaultValue : value;
        }

        public static void AddOrReplaceValue(this IDictionary<string, string> dictionary, string key, string value)
        {
            if (dictionary.ContainsKey(key)) dictionary.Remove(key);
            dictionary.Add(key, value);
        }

        public static DateTime GetDateValue(this IDictionary<string, string> dictionary, string key, string defaultValue)
        {
            var value = GetValue(dictionary, key, defaultValue);
            return !string.IsNullOrEmpty(value) 
                ? DateTime.ParseExact(value, "yyyy-MM-dd HH:mm", CultureInfo.InstalledUICulture) 
                : DateTime.MinValue;
        }

        public static TimeSpan GetTimeValue(this IDictionary<string, string> dictionary, string key)
        {
            var value = GetValue(dictionary, key, "18:00");
            return TimeSpan.ParseExact(value, "hh\\:mm", CultureInfo.InstalledUICulture);
        }
    }
}