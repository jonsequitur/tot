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

        public Dictionary<string, string> Files { get; } = new Dictionary<string, string>();

        public override void AppendValues(string seriesName, DateTime time, string[] values)
        {
            var seriesDefinition = GetSeriesDefinitionOrThrow(seriesName);

            seriesDefinition.ValidateValues(values);

            Files[seriesDefinition.Path] = ReadCsv(seriesDefinition.Path) + CreateTimeStampedCsvLine(time, values);
        }

        public override void CreateSeries(string seriesName, string[] columnNames)
        {
            var seriesDefinition = new SeriesDefinition(seriesName, columnNames);

            ThrowIfSeriesIsDefined(seriesDefinition);

            Files[seriesDefinition.Path] = CreateCsvHeaderForSeries(columnNames);
        }

        public override IEnumerable<string> ListSeries() =>
            Files.Keys.Select(Path.GetFileNameWithoutExtension);

        public override string ReadCsv(string series)
        {
            var definition = GetSeriesDefinitionOrThrow(series);

            if (Files.TryGetValue(definition.Path, out var content))
            {
                return content;
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
                var columnNames = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                         .First()
                                         .Split(',');

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