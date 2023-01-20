using System;
using System.Collections.Generic;
using System.Linq;

namespace totlib;

public abstract class DataAccessorBase : IDataAccessor
{
    protected DataAccessorBase(IClock clock = null)
    {
        Clock = clock ?? SystemClock.Instance;
    }

    public IClock Clock { get; }

    public abstract void AppendValues(string seriesName, DateTime time, string[] values);

    public abstract void CreateSeries(string seriesName, string[] columnNames);

    public abstract IEnumerable<string> ListSeries();

    public abstract IEnumerable<string> ReadLines(string series);

    protected SeriesDefinition GetSeriesDefinitionOrThrow(string seriesName)
    {
        if (!TryGetSeriesDefinition(seriesName, out var seriesDefinition))
        {
            throw new TotException($"Series \"{seriesName}\" hasn't been defined. Use tot add to define it.");
        }

        return seriesDefinition;
    }

    protected void ThrowIfSeriesIsDefined(SeriesDefinition series)
    {
        if (TryGetSeriesDefinition(series.Path, out _))
        {
            throw new TotException($"Series \"{series.Name}\" has already been defined.");
        }
    }

    protected string CreateTimeStampedCsvLine(DateTime time, IEnumerable<string> values)
    {
        if (time == default)
        {
            time = Clock.Now;
        }

        var timestamp = new[] { time.ToString("s") };

        if (values != null)
        {
            values = timestamp.Concat(values);
        }
        else
        {
            values = timestamp;
        }

        return string.Join(",", values);
    }

    protected string CreateCsvHeaderForSeries(IEnumerable<string> columnNames)
    {
        var time = new[] { "time" };

        if (columnNames != null)
        {
            columnNames = time.Concat(columnNames);
        }
        else
        {
            columnNames = time;
        }

        return string.Join(",", columnNames);
    }

    protected abstract bool TryGetSeriesDefinition(string name, out SeriesDefinition seriesDefinition);
}