using System.Collections.Generic;
using System.Linq;

namespace totlib
{
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
                        throw new TotException($"Too many values specified. Series \"{Name}\" expects values for: {expectedColumns}");
                    }
                }

                if (valuesLength < colCountWithoutTime)
                {
                    throw new TotException($"Too few values specified. Series \"{Name}\" expects values for: {expectedColumns}");
                }
            }

            if (values?.FirstOrDefault(v => v.Contains(",")) is { } hasComma)
            {
                throw new TotException($"Values can't contain commas but this does: \"{hasComma}\"");
            }
            
            if (values?.FirstOrDefault(v => v.Contains("\n")) is { } hasNewline)
            {
                throw new TotException($"Values can't contain newlines but this does: \"{hasNewline}\"");
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