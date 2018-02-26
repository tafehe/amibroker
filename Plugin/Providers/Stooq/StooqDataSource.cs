using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AmiBroker.Plugin.Models;

namespace AmiBroker.Plugin.Providers.Stooq
{
    internal class StooqDataSource : DataSource
    {
        private readonly Regex _linePattern = new Regex(@"^[0-9,\.,-]+$");
        private readonly string _databasePath;

        public StooqDataSource(string databasePath, IntPtr mainWnd) : base(databasePath, mainWnd)
        {
            _databasePath = databasePath;
        }

        public override Quotation[] GetQuotes(string ticker, Periodicity periodicity, int limit)
        {
            var result = GetQuotes(ticker);

            var resultCount = result.Count();
            if (resultCount > limit)
            {
                result = result.Skip(Math.Max(0, resultCount - limit));
            }

            return result.ToArray();
        }

        private IEnumerable<Quotation> GetQuotes(string ticker)
        {
            var configuration = new Configuration(_databasePath, ticker);
            return new StooqDataLoader
            (
                new LocalFileLoader(configuration),
                new EodFileLoader(configuration),
                new IntradayFileLoader(configuration)
            )
            .LoadFile()
            .Where(x => _linePattern.IsMatch(x))
            .Select(line =>
            {
                var value = line.Split(',');
                return new Quotation
                {
                    DateTime = new AmiDate(
                        DateTime.ParseExact(value[0].Replace("-", ""), "yyyyMMdd", CultureInfo.InvariantCulture),
                        true
                    ),
                    Open = Convert.ToSingle(value[1], CultureInfo.InvariantCulture),
                    High = Convert.ToSingle(value[2], CultureInfo.InvariantCulture),
                    Low = Convert.ToSingle(value[3], CultureInfo.InvariantCulture),
                    Price = Convert.ToSingle(value[4], CultureInfo.InvariantCulture),
                    // not always volume is given in the file
                    Volume = value.Length < 6 ? 0 : Convert.ToSingle(value[5], CultureInfo.InvariantCulture)
                };
            });
        }
    }
}