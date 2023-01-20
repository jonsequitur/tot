using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static System.Environment;
using static System.IO.Directory;

namespace totlib;

public class FileBasedDataAccessor : DataAccessorBase
{
    public FileBasedDataAccessor(DirectoryInfo directory = null, IClock clock = null) : base(clock)
    {
        if (directory is null)
        {
            directory = new DirectoryInfo(GetCurrentDirectory());
        }
        else if (!directory.Exists)
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

        File.AppendAllText(fullPath, CreateTimeStampedCsvLine(time, values) + NewLine);
    }

    public override void CreateSeries(string seriesName, string[] columnNames)
    {
        var definition = new SeriesDefinition(seriesName, columnNames);

        ThrowIfSeriesIsDefined(definition);

        var fullPath = Path.Combine(Directory.FullName, SeriesDefinition.GetPath(seriesName));

        File.AppendAllText(fullPath, CreateCsvHeaderForSeries(columnNames) + NewLine);
    }

    public override IEnumerable<string> ListSeries() =>
        Directory.EnumerateFiles("*.csv")
                 .Select(f => Path.GetFileNameWithoutExtension(f.Name));

    public override IEnumerable<string> ReadLines(string series)
    {
        IEnumerator<string> enumerator;

        try
        {
            enumerator = File.ReadLines(GetFullPathForSeries(series)).GetEnumerator();
        }
        catch (FileNotFoundException)
        {
            throw new TotException($"Series \"{series}\" hasn't been defined. Use tot add to define it.");
        }

        using (enumerator)
        {
            while (enumerator.MoveNext())
            {
                var line = enumerator.Current;

                if (!string.IsNullOrEmpty(line))
                {
                    yield return line;
                }
            }
        }
    }

    protected override bool TryGetSeriesDefinition(string name, out SeriesDefinition seriesDefinition)
    {
        if (File.Exists(GetFullPathForSeries(name)))
        {
            var columnNames = ReadLines(name).First().Split(',');

            seriesDefinition = new SeriesDefinition(name, columnNames);
            return true;
        }
        else
        {
            seriesDefinition = default;
            return false;
        }
    }

    private string GetFullPathForSeries(string name)
    {
        return Path.Combine(Directory.FullName, SeriesDefinition.GetPath(name));
    }
}