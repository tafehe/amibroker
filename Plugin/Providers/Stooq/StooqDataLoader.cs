using System;
using System.Collections;
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
        private const string DownloadLastRun = "LAST_DOWNLOAD_RUN";
        private const string LastEntryInFile = "LAST_ENTRY_IN_FILE";
        private const string SecondTimeCheckAfter = "SECOND_CHECK_ON_OR_AFTER";
        private const string DefaultStartDate = "1970-01-01 00:00";
        private const string EodUrl =     "https://stooq.pl/q/d/l/?s={0}&d1={1}&d2={2}&i=d";
        private const string IntradyUrl = "https://stooq.pl/q/l/?s={0}&f=d1ohlcv";
        private readonly Regex _linePatter = new Regex(@"^[0-9,\.,-]+$");
        private readonly Dictionary<string, string> _config;
        private readonly string _configFile;
        private readonly string _file;
        private readonly string _ticker;

        public StooqDataLoader(string ticker, string databasePath)
        {
            _ticker = ticker;
            _file = databasePath + @"\" + ticker + ".csv";
            _configFile = databasePath + @"\" + ticker + ".config";
            _config = LoadConfig();
        }

        public IEnumerable<string> LoadFile()
        {
            var result = LoadLocalFile();
            IEnumerable<string> remoteFile;
            if (IsRefreshNeeded())
            {
                remoteFile = LoadRemoteEodFile();
                result = Merge(result, remoteFile);
            }
            remoteFile = LoadRemoteIntradayFile();

            SaveFile(result);
            SaveConfig();

            return Merge(result, remoteFile);
        }

        private IEnumerable<string> Merge(IEnumerable<string> first, IEnumerable<string> second)
        {
            // first line of the stooq file has to be skipped
            if (second.Count() < 2) return first;
            if (first.Count() < 2) return second;

            var validatedSecond = second.Where(x => IsValid(x));

            if (!validatedSecond.Any()) return first;

            var firstDateInSecond = GetStartDateAsLong(validatedSecond.First());

            // find the last entry in first list which is older then the first entry in the second list
            var validatedFirst = first
                .Where(x => !String.IsNullOrEmpty(x))
                .TakeWhile(x => GetStartDateAsLong(x) < firstDateInSecond).ToList();
            validatedFirst.AddRange(validatedSecond);

            return validatedFirst;
        }

        private long GetStartDateAsLong(string line)
        {
            if (IsValid(line)) 
            {
                return Convert.ToInt64(line.Split(',')[0].Replace("-", ""));
            }

            return String.IsNullOrEmpty(line) ? 99999999L : -1L;
        }

        private IEnumerable<string> LoadRemoteEodFile()
        {
            var startDate = _config.GetValue(LastEntryInFile, "19700101").Replace("-", "");
            var endDate = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
            var url = String.Format(EodUrl, _ticker, startDate, endDate);

            return GetUrlReponse(url);
        } 

        private IEnumerable<string> LoadRemoteIntradayFile()
        {
            var url = String.Format(IntradyUrl, _ticker);

            return GetUrlReponse(url);
        }

        private IEnumerable<string> GetUrlReponse(string url)
        {
            try
            {
                var request = WebRequest.Create(url);
                var response = request.GetResponse();

                var dataStream = response.GetResponseStream();
                // Open the stream using a StreamReader for easy access.
                if (dataStream != null)
                {
                    var reader = new StreamReader(dataStream);
                    // Read the content.
                    var responseFromServer = reader.ReadToEnd();

                    return Regex.Split(responseFromServer, @"\r\n").ToList();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }

            return Enumerable.Empty<string>();
        }

        private IEnumerable<string> LoadLocalFile()
        {
            if (File.Exists(_file)) {
                var fileContent = File.ReadAllLines(_file);

                var lastLine = fileContent.Where(x => !String.IsNullOrEmpty(x))
                    .DefaultIfEmpty(String.Empty)
                    .Select(x => x.Split(',')[0])
                    .Last();
                _config.AddOrReplaceValue(LastEntryInFile, lastLine);

                return fileContent.ToList();
            }

            return Enumerable.Empty<string>();
        }
        

        private bool IsValid(string line)
        {
            if (String.IsNullOrEmpty(line))
            {
                return false;
            }

            return _linePatter.IsMatch(line);
        }

        private Boolean IsRefreshNeeded()
        {
            var downloadLastRun = _config.GetDateValue(DownloadLastRun, DefaultStartDate);
            var now = DateTime.Now;

            if (now.Date > downloadLastRun.Date) return true;

            var secondCheckTime = _config.GetTimeValue(SecondTimeCheckAfter);

            // if last downloaded time was before second time download check
            // and now is after second time check then refresh is needed.
            return downloadLastRun.TimeOfDay <= secondCheckTime && secondCheckTime <= now.TimeOfDay;
        }

        private void SaveFile(IEnumerable<string> fileContent)
        {
            _config.AddOrReplaceValue(DownloadLastRun, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            File.WriteAllLines(_file, fileContent);
        }

        private Dictionary<string, string> LoadConfig()
        {
            var data = new Dictionary<string, string>();
            if (File.Exists(_configFile))
            {
                foreach (var row in File.ReadAllLines(_configFile))
                {
                    data.Add(row.Split('=')[0], String.Join("=", row.Split('=').Skip(1).ToArray()));
                }
            }

            return data;
        }

        private void SaveConfig()
        {
            var configLines = _config.Select(keyValue => keyValue.Key + "=" + keyValue.Value).ToList();
            File.WriteAllLines(_configFile, configLines);
        }
    }

    public static class LocalExtentions
    {
        public static string GetValue(this Dictionary<string, string> dictionary, string key, string defaultValue)
        {
            if (!dictionary.TryGetValue(key, out string value))
            {
                value = defaultValue;
            }

            return value;
        }

        public static void AddOrReplaceValue(this Dictionary<string, string> dictionary, string key, string value)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary.Remove(key);
            }

            dictionary.Add(key, value);
        }

        public static DateTime GetDateValue(this Dictionary<string, string> dictionary, string key, string defaultValue)
        {
            var value = GetValue(dictionary, key, defaultValue);
            if (!String.IsNullOrEmpty(value))
            {
                return DateTime.ParseExact(value, "yyyy-MM-dd HH:mm", CultureInfo.InstalledUICulture);
            }

            return DateTime.MinValue;
        }

        public static TimeSpan GetTimeValue(this Dictionary<string, string> dictionary, string key)
        {
            var value = GetValue(dictionary, key, "18:00");
            return TimeSpan.ParseExact(value, "hh\\:mm", CultureInfo.InstalledUICulture);
        }
    }
}