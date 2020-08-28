using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using totlib;

namespace tot
{
    public class Program
    {
        static async Task<int> Main(string[] args)
        {
            return await CommandLineParser.InvokeAsync(args);
        }

        public static Parser CommandLineParser { get; } =
            CreateCommandLineParser();

        public static Parser CreateCommandLineParser(IDataAccessor dataAccessor = null)
        {
            var pathOption = new Option<DirectoryInfo>(
                "--path",
                description: "The path containing the time series",
                getDefaultValue: () => new DirectoryInfo(Directory.GetCurrentDirectory()));

            var timeOption = new Option<DateTime>(
                "--time",
                description: "The time to record with the event");

            var rootCommand = new RootCommand("tot")
            {
                Add(),
                List(),
                pathOption,
                timeOption,
                new Argument<string>("series").AddSuggestions((result, match) =>
                {
                    if (result == null)
                    {
                        return Array.Empty<string>();
                    }

                    var path = result.ValueForOption(pathOption);

                    EnsureDataAccessorIsInitialized(path, ref dataAccessor);

                    return dataAccessor.ListSeries();
                }),
                new Argument<IEnumerable<string>>("values")
            };

            rootCommand.Handler = CommandHandler.Create<string, string[], DateTime, DirectoryInfo>((series, values, time, path) =>
            {
                EnsureDataAccessorIsInitialized(path, ref dataAccessor);

                dataAccessor.AppendValues(series, time, values);
            });

            Command Add()
            {
                var command = new Command("add", "Adds a new series")
                {
                    new Argument<string>("name"),
                    new Argument<IEnumerable<string>>("columns")
                };

                command.Handler = CommandHandler.Create<string, string[], DirectoryInfo>(async (name, columns, path) =>
                {
                    EnsureDataAccessorIsInitialized(path, ref dataAccessor);

                    dataAccessor.CreateSeries(name, columns);
                });

                return command;
            }

            Command List()
            {
                var command = new Command("list", "Lists the defined series");

                command.Handler = CommandHandler.Create<DirectoryInfo, IConsole>((path, console) =>
                {
                    EnsureDataAccessorIsInitialized(path, ref dataAccessor);

                    var series = dataAccessor.ListSeries().OrderBy(s => s);

                    console.Out.Write(
                        string.Join(
                            Environment.NewLine, series));
                });

                return command;
            }

            var builder = new CommandLineBuilder(rootCommand)
                .UseDefaults();

            return builder.Build();
        }

        private static void EnsureDataAccessorIsInitialized(
            DirectoryInfo path,
            ref IDataAccessor dataAccessor)
        {
            dataAccessor ??=
                new FileBasedDataAccessor(
                    path,
                    new SystemClock());
        }
    }
}