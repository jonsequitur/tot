using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace totlib
{
    public class FileBasedDataAccessor : DataAccessorBase
    {
        public FileBasedDataAccessor(DirectoryInfo directory, IClock clock) : base(clock)
        {
            if (!directory.Exists)
            {
                throw new ArgumentException($"Directory does not exist: {directory}");
            }

            Directory = directory;
        }

        public DirectoryInfo Directory { get; set; }

        public override void AppendValues(string seriesName, DateTime time, string[] values)
        {
            var seriesDefinition = GetSeriesDefinitionOrThrow(seriesName);

            seriesDefinition.ValidateValues(values);

            var fullPath = Path.Combine(Directory.FullName, seriesDefinition.Path);

            File.AppendAllText(fullPath, CreateTimeStampedCsvLine(time, values));
        }

        public override void CreateSeries(string seriesName, string[] columnNames)
        {
            var definition = new SeriesDefinition(seriesName, columnNames);

            ThrowIfSeriesIsDefined(definition);

            var fullPath = Path.Combine(Directory.FullName, SeriesDefinition.GetPath(seriesName));

            File.AppendAllText(fullPath, CreateCsvHeaderForSeries(columnNames));
        }

        public override IEnumerable<string> ListSeries() =>
            Directory.EnumerateFiles("*.csv")
                     .Select(f => Path.GetFileNameWithoutExtension(f.Name));

        public override string ReadCsv(string series)
        {
            var definition = GetSeriesDefinitionOrThrow(series);

            var fullPath = Path.Combine(Directory.FullName, definition.Path);

            return File.ReadAllText(fullPath);
        }

        protected override bool TryGetSeriesDefinition(string name, out SeriesDefinition seriesDefinition)
        {
            var fullPath = Path.Combine(Directory.FullName, SeriesDefinition.GetPath(name));

            if (File.Exists(fullPath))
            {
                var columnNames = File.ReadLines(fullPath).First().Split(',');

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

    public class SeriesDefinition
    {
        public SeriesDefinition(string name, IReadOnlyList<string> columnNames)
        {
            Name = name;
            Path = GetPath(name);
            ColumnNames = columnNames;
        }

        public string Name { get; }

        public string Path { get; }

        public IReadOnlyList<string> ColumnNames { get; }

        public void ValidateValues(string[] values)
        {
            var colCountWithoutTime = ColumnNames.Count - 1;

            var valuesLength = values?.Length ?? 0;

            if (valuesLength != colCountWithoutTime)
            {
                var expectedColumns = string.Join(",", ColumnNames.Skip(1));

                if (valuesLength > colCountWithoutTime)
                {
                    if (colCountWithoutTime == 0)
                    {
                        throw new TotException($"Too many values specified. Series \"{Name}\" expects none.");
                    }
                    else
                    {
                        throw new TotException($"Too many values specified. Series \"{Name}\" expects values: {expectedColumns}");
                    }
                }

                if (valuesLength < colCountWithoutTime)
                {
                    throw new TotException($"Too few values specified. Series \"{Name}\" expects values: {expectedColumns}");
                }
            }
        }

        public static string GetPath(string name)
        {
            if (string.IsNullOrWhiteSpace(System.IO.Path.GetExtension(name)))
            {
                return name + ".csv";
            }
            else
            {
                return name;
            }
        }
    }
}