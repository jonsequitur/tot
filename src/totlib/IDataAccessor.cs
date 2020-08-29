using System;
using System.Collections.Generic;

namespace totlib
{
    public interface IDataAccessor
    {
        string ReadCsv(string series);

        void AppendValues(string seriesName, DateTime time, string[] values);

        void CreateSeries(string seriesName, string[] columnNames);

        IEnumerable<string> ListSeries();

        IClock Clock { get; }
    }

    public static class DataAccessor
    {
        public static void AppendValues(
            this IDataAccessor dataAccessor,
            string seriesName,
            params string[] values)
        {
            dataAccessor.AppendValues(seriesName, default,  values);
        }

        public static void CreateSeries(
            this IDataAccessor dataAccessor,
            string seriesName,
            params string[] columnNames)
        {
            dataAccessor.CreateSeries(seriesName, columnNames);
        }
    }
}