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
        private const string DownloadLastRun = "LAST_DOWNLOAD_RUN";
        private const string LastEntryInFile = "LAST_ENTRY_IN_FILE";
        private const string SecondTimeCheckAfter = "SECOND_CHECK_ON_OR_AFTER";
        private const string DefaultStartDate = "1970-01-01 00:00";
        private const string EodUrl =     @"http://stooq.pl/q/d/l/?s={0}&d1={1}&d2={2}&i=d";
        private const string IntradyUrl = @"http://stooq.pl/q/l/?s={0}&f=d1ohlcv";
        private readonly Regex LinePatter = new Regex(@"^[0-9,\.,-]+$");
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

        public List<string> LoadFile()
        {
            var result = LoadLocalFile();
            List<string> remoteFile;
            if (IsRefreshNeeded())
            {
                remoteFile = LoadRemoteEODFile();
                result = Merge(result, remoteFile);
            }
            remoteFile = LoadRemoteIntradayFile();
            result = Merge(result, remoteFile);

            result = Merge(result, remoteFile);
            SaveFile(result);
            SaveConfig();

            return result;
        }

        private List<string> Merge(List<string> first, List<string> second)
        {
            // first line of the stooq file has to be skipped
            if (second.Count < 2) return first;
            if (first.Count < 2) return second;

            var validLineIndex = GetFirstValidLineIndex(second);
            if (validLineIndex == -1) return first;

            var firstDateInSecond = Convert.ToInt64(second[validLineIndex].Split(',')[0].Replace("-", ""));

            // find the last entry in one which is older then then first entry in second list
            int i;
            for (i = first.Count - 1; i > first.Count - second.Count - 1; i--)
            {
                if (!IsValid(first[i])) continue;

                var secondDate = Convert.ToInt64(first[i].Split(',')[0].Replace("-", ""));
                if (secondDate < firstDateInSecond)
                {
                    break;
                }
            }

            var result = first.GetRange(0, i + 1);
            result.AddRange(second.GetRange(validLineIndex, second.Count - 1));

            return result;
        }

        private int GetFirstValidLineIndex(List<string> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (IsValid(list[i])) return i;
            }

            return -1;
        }

        private List<string> LoadRemoteEODFile()
        {
            var startDate = _config.GetValue(LastEntryInFile, "19700101").Replace("-", "");
            var endDate = DateTime.Now.ToString("yyyyMMdd");
            var url = String.Format(EodUrl, _ticker, startDate, endDate);

            return GetUrlReponse(url);
        } 

        private List<string> LoadRemoteIntradayFile()
        {
            var url = String.Format(IntradyUrl, _ticker);

            return GetUrlReponse(url);
        }

        private List<string> GetUrlReponse(string url)
        {
            try
            {
                var request = WebRequest.Create(url);
                var response = request.GetResponse();

                var dataStream = response.GetResponseStream();
                // Open the stream using a StreamReader for easy access.
                var reader = new StreamReader(dataStream);
                // Read the content.
                var responseFromServer = reader.ReadToEnd();

                return Regex.Split(responseFromServer, @"\r\n").ToList();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }

            return new List<string>();
        }

        private List<string> LoadLocalFile()
        {
            if (File.Exists(_file))
            {
                var fileContent = File.ReadAllLines(_file).ToList();

                if (fileContent.Count > 0)
                {
                    for (var i = fileContent.Count - 1; i > 0; i--)
                    {
                        if (!String.IsNullOrEmpty(fileContent[i]))
                        {
                            var lastLine = fileContent[i].Split(',')[0];
                            _config.AddOrReplaceValue(LastEntryInFile, lastLine);
                            break;
                        }
                    }
                }
                return fileContent;
            }
            return new List<string>();
        }

        private bool IsValid(string line)
        {
            if (String.IsNullOrEmpty(line))
            {
                return false;
            }

            return LinePatter.IsMatch(line);
        }

        private Boolean IsRefreshNeeded()
        {
            DateTime downloadLastRun = _config.GetDateValue(DownloadLastRun, DefaultStartDate);
            DateTime now = DateTime.Now;

            if (now.Date > downloadLastRun.Date) return true;

            TimeSpan secondCheckTime = _config.GetTimeValue(SecondTimeCheckAfter);

            // if last downloaded time was before second time download check
            // and now is after second time check then refresh is needed.
            if (downloadLastRun.TimeOfDay <= secondCheckTime && secondCheckTime <= now.TimeOfDay) return true;

            return false;
        }

        private void SaveFile(List<string> fileContent)
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
                    data.Add(row.Split('=')[0], String.Join("=", row.Split('=').Skip(1).ToArray()));
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
            string value;
            if (!dictionary.TryGetValue(key, out value))
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