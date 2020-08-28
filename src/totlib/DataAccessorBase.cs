﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace totlib
{
    public abstract class DataAccessorBase : IDataAccessor
    {
        private readonly IClock _clock;

        protected DataAccessorBase(IClock clock)
        {
            _clock = clock;
        }

        public abstract void AppendValues(string seriesName, DateTime time, string[] values);

        public abstract void CreateSeries(string seriesName, string[] columnNames);

        public abstract IEnumerable<string> ListSeries();

        public abstract string ReadCsv(string series);

        protected SeriesDefinition GetSeriesDefinitionOrThrow(string seriesName)
        {
            if (!TryGetSeriesDefinition(seriesName, out var seriesDefinition))
            {
                throw new ArgumentException($"Series \"{seriesName}\" hasn't been defined. Use tot add to define it.");
            }

            return seriesDefinition;
        }

        protected void ThrowIfSeriesIsDefined(SeriesDefinition series)
        {
            if (TryGetSeriesDefinition(series.Path, out _))
            {
                throw new ArgumentException($"Series \"{series.Name}\" has already been defined.");
            }
        }

        protected string CreateTimeStampedCsvLine(DateTime time, IEnumerable<string> values)
        {
            if (time == default)
            {
                time = _clock.Now;
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

            return string.Join(",", values) + Environment.NewLine;
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

            return string.Join(",", columnNames) + Environment.NewLine;
        }

        protected abstract bool TryGetSeriesDefinition(string name, out SeriesDefinition seriesDefinition);
    }
}