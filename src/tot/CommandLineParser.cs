using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Globalization;
using System.IO;
using Humanizer;
using System.Linq;
using TimeSpanParserUtil;
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

            var timeOption = new Option<DateTime?>(
                new[] { "-t", "--time" },
                description: "The time to record with the event",
                parseArgument: ParseTimeOrDuration(GetDataAccessor),
                isDefault: true);

            var rootCommand = new RootCommand("tot")
            {
                AddCommand(),
                ListCommand(),
                timeOption,
                new Argument<string>("series").AddSuggestions(SuggestSeriesName),
                new Argument<IEnumerable<string>>("values")
            };

            rootCommand.AddGlobalOption(pathOption);

            rootCommand.Handler = CommandHandler.Create<string, string[], DateTime?, DirectoryInfo, IConsole>((series, values, time, path, console) =>
            {
                var accessor = GetDataAccessor(path);

                time ??= accessor.Clock.Now;

                accessor.AppendValues(series, time.Value, values);

                console.Out.WriteLine($"{time.Humanize(utcDate: false)}: {series} {string.Join(" ", values ?? Array.Empty<string>())}");
            });

            Command AddCommand()
            {
                var command = new Command("add", "Adds a new series")
                {
                    new Argument<string>("name"),
                    new Argument<IEnumerable<string>>("columns")
                };

                command.Handler = CommandHandler.Create<string, string[], DirectoryInfo>(async (name, columns, path) =>
                                                                                             GetDataAccessor(path).CreateSeries(name, columns));

                return command;
            }

            Command ListCommand()
            {
                var command = new Command("list", "Lists the defined series")
                {
                    new Argument<string>("series")
                    {
                        Arity = ArgumentArity.ZeroOrOne
                    }.AddSuggestions(SuggestSeriesName),
                    timeOption
                };

                command.Handler = CommandHandler.Create<DirectoryInfo, string, DateTime?, IConsole>((path, series, time, console) =>
                {
                    if (string.IsNullOrEmpty(series))
                    {
                        // list the known series names
                        var seriesNames = GetDataAccessor(path).ListSeries().OrderBy(s => s);

                        console.Out.Write(
                            string.Join(
                                Environment.NewLine, seriesNames));
                    }
                    else
                    {
                        // list the contents of the specified series
                        var readLines = GetDataAccessor(path)
                                        .ReadLines(series)
                                        .Skip(1); // skip the heading row

                        if (time is {} specified)
                        {
                            var specificDay = specified.Date == time;

                            readLines = readLines
                                        .Select(line =>
                                        {
                                            var timestampString = line.Split(',')[0];

                                            var timestamp = DateTime.Parse(timestampString);

                                            return (timestamp, line);
                                        })
                                        .Where(t => specificDay 
                                                        ? t.timestamp.Date == specified 
                                                        : t.timestamp >= specified)
                                        .OrderBy(t => t.timestamp)
                                        .Select(t => t.line);
                        }

                        var lines = string.Join(Environment.NewLine, readLines);

                        console.Out.WriteLine(lines);
                    }
                });

                return command;
            }

            var builder = new CommandLineBuilder(rootCommand)
                          .DisplayException()
                          .UseDefaults();

            return builder.Build();

            IEnumerable<string> SuggestSeriesName(ParseResult result, string match)
            {
                if (result == null)
                {
                    return Array.Empty<string>();
                }

                var path = result.ValueForOption(pathOption);

                var accessor = GetDataAccessor(path);

                return accessor.ListSeries();
            }

            IDataAccessor GetDataAccessor(DirectoryInfo path) =>
                dataAccessor ??= new FileBasedDataAccessor(path, SystemClock.Instance);

            ParseArgument<DateTime?> ParseTimeOrDuration(Func<DirectoryInfo, IDataAccessor> getDataAccessor)
            {
                return result =>
                {
                    var token = result.Tokens.SingleOrDefault()?.Value;

                    var path = result.FindValueForOption(pathOption);

                    var now = (getDataAccessor(path)?.Clock ?? SystemClock.Instance).Now;

                    if (token is null)
                    {
                        return default;
                    }

                    if (DateTime.TryParse(token, provider: null, styles: DateTimeStyles.NoCurrentDateDefault, out var specified))
                    {
                        if (specified.Date == default)
                        {
                            if (now.TimeOfDay < specified.TimeOfDay)
                            {
                                var yesterday = now.Subtract(TimeSpan.FromDays(1));
                                return yesterday.Date.Add(specified.TimeOfDay);
                            }
                            else
                            {
                                return now.Date.Add(specified.TimeOfDay);
                            }
                        }

                        return specified;
                    }

                    if (TimeSpanParser.TryParse(token, out var timespan))
                    {
                        return now.Add(timespan);
                    }

                    result.ErrorMessage = $"Couldn't figure out what time \"{token}\" refers to.";

                    return default;
                };
            }
        }

        public static T FindValueForArgument<T>(
            this ArgumentResult argumentResult,
            Argument<T> argument)
        {
            var result = argumentResult.FindResultFor(argument);

            if (result is {})
            {
                return result.GetValueOrDefault<T>();
            }

            return default;
        }

        public static T FindValueForOption<T>(
            this ArgumentResult optionResult,
            Option<T> option)
        {
            var result = optionResult.FindResultFor(option);

            if (result is {})
            {
                return result.GetValueOrDefault<T>();
            }

            return default;
        }
    }
}