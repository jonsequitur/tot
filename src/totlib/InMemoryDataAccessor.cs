using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace totlib
{
    public class InMemoryDataAccessor : DataAccessorBase
    {
        public InMemoryDataAccessor(IClock clock) : base(clock)
        {
        }

        public Dictionary<string, List<string>> Files { get; } = new Dictionary<string, List<string>>();

        public override void AppendValues(string seriesName, DateTime time, string[] values)
        {
            var seriesDefinition = GetSeriesDefinitionOrThrow(seriesName);

            seriesDefinition.ValidateValues(values);

            if (!Files.TryGetValue(seriesDefinition.Path, out var lines))
            {
                lines = new List<string>();
                Files[seriesDefinition.Path] = lines;
            }

            lines.Add(CreateTimeStampedCsvLine(time, values));
        }

        public override void CreateSeries(string seriesName, string[] columnNames)
        {
            var seriesDefinition = new SeriesDefinition(seriesName, columnNames);

            ThrowIfSeriesIsDefined(seriesDefinition);

            Files[seriesDefinition.Path] =
                new List<string>
                {
                    CreateCsvHeaderForSeries(columnNames)
                };
        }

        public override IEnumerable<string> ListSeries() =>
            Files.Keys.Select(Path.GetFileNameWithoutExtension);

        public override IEnumerable<string> ReadLines(string series)
        {
            var definition = GetSeriesDefinitionOrThrow(series);

            if (Files.TryGetValue(definition.Path, out var lines))
            {
                return lines;
            }
            else
            {
                return null;
            }
        }

        protected override bool TryGetSeriesDefinition(string name, out SeriesDefinition seriesDefinition)
        {
            var path = SeriesDefinition.GetPath(name);

            if (Files.TryGetValue(path, out var content))
            {
                var columnNames = content.First().Split(',');

                seriesDefinition = new SeriesDefinition(name, columnNames);
                return true;
            }

            else
            {
                seriesDefinition = default;
                return false;
            }
        }
    }
}