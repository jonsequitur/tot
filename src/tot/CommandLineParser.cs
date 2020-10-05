using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using Humanizer;
using totlib;

namespace tot
{
    public static class CommandLineParser
    {
        public static Parser Create(IDataAccessor dataAccessor = null)
        {
            var pathOption = new Option<DirectoryInfo>(
                "--path",
                description: "The path containing the time series",
                getDefaultValue: () => new DirectoryInfo(Directory.GetCurrentDirectory()));

            var rootCommand = new RootCommand("tot")
            {
                AddCommand(),
                ListCommand(),
                TimeOption(),
                SeriesArgument(),
                new Argument<IEnumerable<string>>("values")
            };

            rootCommand.AddGlobalOption(pathOption);

            rootCommand.Handler = CommandHandler.Create<string, string[], DateTime, DirectoryInfo, IConsole>((series, values, time, path, console) =>
            {
                Program.EnsureDataAccessorIsInitialized(path, ref dataAccessor);

                dataAccessor.AppendValues(series, time, values);

                console.Out.WriteLine($"{time.Humanize(utcDate: false)}: {series} {String.Join(" ", values ?? Array.Empty<string>())}");
            });

            Command AddCommand()
            {
                var command = new Command("add", "Adds a new series")
                {
                    new Argument<string>("name"),
                    new Argument<IEnumerable<string>>("columns")
                };

                command.Handler = CommandHandler.Create<string, string[], DirectoryInfo>(async (name, columns, path) =>
                {
                    Program.EnsureDataAccessorIsInitialized(path, ref dataAccessor);

                    dataAccessor.CreateSeries(name, columns);
                });

                return command;
            }

            Command ListCommand()
            {
                var command = new Command("list", "Lists the defined series");

                command.Handler = CommandHandler.Create<DirectoryInfo, IConsole>((path, console) =>
                {
                    Program.EnsureDataAccessorIsInitialized(path, ref dataAccessor);

                    var series = dataAccessor.ListSeries().OrderBy(s => s);

                    console.Out.Write(
                        String.Join(
                            Environment.NewLine, series));
                });

                return command;
            }

            var builder = new CommandLineBuilder(rootCommand)
                          .DisplayException()
                          .UseDefaults();

            return builder.Build();

            Option<DateTime> TimeOption()
            {
                return new Option<DateTime>(
                    new[] { "-t", "--time" },
                    description: "The time to record with the event",
                    parseArgument: Program.ParseTimeArgument(dataAccessor),
                    isDefault: true);
            }

            Argument<string> SeriesArgument()
            {
                return new Argument<string>("series").AddSuggestions((result, match) =>
                {
                    if (result == null)
                    {
                        return Array.Empty<string>();
                    }

                    var path = result.ValueForOption(pathOption);

                    Program.EnsureDataAccessorIsInitialized(path, ref dataAccessor);

                    return dataAccessor.ListSeries();
                });
            }
        }
    }
}