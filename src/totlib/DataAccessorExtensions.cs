using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace totlib
{
    public static class DataAccessorExtensions
    {
        public static IEnumerable<CsvSeriesEntry> ReadSeriesData(
            this IDataAccessor accessor,
            string series)
        {
            var readLines =
                accessor
                    .ReadLines(series) // skip the heading row
                    .Skip(1)
                    .Select(line =>
                    {
                        var timestampString = line.Split(',')[0];

                        var timestamp = DateTime.ParseExact(
                           timestampString, "s", CultureInfo.InvariantCulture);

                        return new CsvSeriesEntry
                        {
                            Line = line,
                            Timestamp = timestamp
                        };
                    })
                    .OrderBy(t => t.Timestamp);

            return readLines;
        }
    }

    public struct CsvSeriesEntry
    {
        public string Line { get; internal set; }

        public DateTime Timestamp { get; internal set; }
    }
}