using System;
using System.Collections.Generic;
using System.Globalization;

namespace AmiBroker.Plugin.Providers.Stooq
{
    public static class Utils
    {
        internal static string GetValue(this IDictionary<string, string> dictionary, string key, string defaultValue)
        {
            return !dictionary.TryGetValue(key, out var value) ? defaultValue : value;
        }

        public static void AddOrReplaceValue(this IDictionary<string, string> dictionary, string key, string value)
        {
            if (dictionary.ContainsKey(key)) dictionary.Remove(key);
            dictionary.Add(key, value);
        }

        internal static int AsInt(this string line)
        {
            return ContentValidator.IsValid(line) 
                ? Convert.ToInt32(line.Split(',')[0].Replace("-", ""))
                : string.IsNullOrEmpty(line) ? 99999999 : -1;
        }

        internal static DateTime AsDateTime(this string value)
        {
            return DateTime.ParseExact(value.Replace("-", ""), "yyyyMMdd", CultureInfo.InvariantCulture);
        }
        
        internal static bool IsWeekend(this DateTime dateTime)
        {
            return dateTime.DayOfWeek == DayOfWeek.Saturday
               || dateTime.DayOfWeek == DayOfWeek.Sunday;
        }
    }
}