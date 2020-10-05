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
}