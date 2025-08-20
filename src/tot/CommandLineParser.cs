using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using TimeSpanParserUtil;
using totlib;

namespace tot;

public static class CommandLineParser
{
    public static RootCommand Create(IDataAccessor? dataAccessor = null)
    {
        var pathOption = new Option<DirectoryInfo>("--path")
        {
            Description = "The path containing the time series"
        };

        var timeOption = new Option<string>("--time", "-t")
        {
            Description = "The time of the event, either as a date (-t \"2020-08-12 3pm\") or as a relative time period (-t -45m)"
        };

        var seriesArg = new Argument<string>("series");
        var valuesArg = new Argument<string[]>("values");

        var rootCommand = new RootCommand("tot")
        {
            AddCommand(),
            ListCommand(),
            LatestCommand(),
            pathOption,
            timeOption,
            seriesArg,
            valuesArg
        };

        // Try to use SetAction like in the rc1 example
        rootCommand.SetAction((parseResult, cancellationToken) =>
        {
            try
            {
                var series = parseResult.GetValue(seriesArg);
                var values = parseResult.GetValue(valuesArg);
                var timeValue = parseResult.GetValue(timeOption);
                var path = parseResult.GetValue(pathOption) ?? new DirectoryInfo(Directory.GetCurrentDirectory());

                var accessor = GetDataAccessor(path);

                // Parse time if provided, otherwise use current time
                DateTime time;
                if (!string.IsNullOrEmpty(timeValue))
                {
                    time = ParseTimeOrDuration(timeValue, accessor.Clock.Now);
                }
                else
                {
                    time = accessor.Clock.Now;
                }

                accessor.AppendValues(series, time, values);

                Console.WriteLine($"{time.Humanize(utcDate: false)}: {series} {string.Join(" ", values ?? Array.Empty<string>())}");
                
                return Task.FromResult(0);
            }
            catch (TotException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(e.Message);
                Console.ResetColor();
                return Task.FromResult(1);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Error: {e.Message}");
                Console.ResetColor();
                return Task.FromResult(1);
            }
        });

        return rootCommand;

        Command AddCommand()
        {
            var nameArg = new Argument<string>("name");
            var columnsArg = new Argument<string[]>("columns");

            var command = new Command("add", "Adds a new series")
            {
                nameArg,
                columnsArg
            };

            command.SetAction((parseResult, cancellationToken) =>
            {
                try
                {
                    var name = parseResult.GetValue(nameArg);
                    var columns = parseResult.GetValue(columnsArg);
                    var path = parseResult.GetValue(pathOption) ?? new DirectoryInfo(Directory.GetCurrentDirectory());
                    
                    GetDataAccessor(path).CreateSeries(name, columns);
                    return Task.FromResult(0);
                }
                catch (TotException e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine(e.Message);
                    Console.ResetColor();
                    return Task.FromResult(1);
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine($"Error: {e.Message}");
                    Console.ResetColor();
                    return Task.FromResult(1);
                }
            });

            return command;
        }

        Command LatestCommand()
        {
            var command = new Command("latest", "Lists the latest entries in each series");
            
            command.SetAction((parseResult, cancellationToken) =>
            {
                try
                {
                    var path = parseResult.GetValue(pathOption) ?? new DirectoryInfo(Directory.GetCurrentDirectory());
                    var dataAccessor = GetDataAccessor(path);

                    foreach (var x in dataAccessor
                                      .ListSeries()
                                      .Select(name => (name, data: dataAccessor.ReadSeriesData(name).LastOrDefault()))
                                      .Where(t => t.data is { })
                                      .OrderBy(t => t.data.Timestamp))
                    {
                        Console.WriteLine($"{x.name}:");
                        Console.WriteLine($"    {x.data.Line}");
                    }
                    
                    return Task.FromResult(0);
                }
                catch (TotException e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine(e.Message);
                    Console.ResetColor();
                    return Task.FromResult(1);
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine($"Error: {e.Message}");
                    Console.ResetColor();
                    return Task.FromResult(1);
                }
            });
            
            return command;
        }

        Command ListCommand()
        {
            var seriesArg = new Argument<string>("series");
            seriesArg.Arity = ArgumentArity.ZeroOrOne;
            var afterOption = new Option<string>("--after", "-a")
            {
                Description = "The start time after which to list events"
            };
            var daysOption = new Option<bool>("--days")
            {
                Description = "List only unique days on which events occurred"
            };

            var command = new Command("list", "Lists the defined series")
            {
                seriesArg,
                afterOption,
                daysOption
            };

            command.SetAction((parseResult, cancellationToken) =>
            {
                try
                {
                    var path = parseResult.GetValue(pathOption) ?? new DirectoryInfo(Directory.GetCurrentDirectory());
                    var series = parseResult.GetValue(seriesArg);
                    var after = parseResult.GetValue(afterOption);
                    var days = parseResult.GetValue(daysOption);
                    
                    var accessor = GetDataAccessor(path);

                    if (string.IsNullOrEmpty(series))
                    {
                        // list the known series names
                        var seriesNames = accessor.ListSeries().OrderBy(s => s);

                        Console.Write(
                            string.Join(
                                Environment.NewLine, seriesNames));
                    }
                    else
                    {
                        var readLines = accessor.ReadSeriesData(series);

                        if (!string.IsNullOrEmpty(after))
                        {
                            var specified = ParseTimeOrDuration(after, accessor.Clock.Now);
                            var specificDay = specified.Date == specified;

                            readLines = readLines
                                .Where(t => specificDay
                                                ? t.Timestamp.Date == specified.Date
                                                : t.Timestamp >= specified);
                        }

                        IEnumerable<string> lines;

                        if (days)
                        {
                            lines = readLines.Select(l => l.Timestamp.Date)
                                             .Distinct()
                                             .Select(t => t.ToString("s"));
                        }
                        else
                        {
                            lines = readLines.Select(l => l.Line);
                        }

                        foreach (var line in lines)
                        {
                            Console.WriteLine(line);
                        }
                    }
                    
                    return Task.FromResult(0);
                }
                catch (TotException e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine(e.Message);
                    Console.ResetColor();
                    return Task.FromResult(1);
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine($"Error: {e.Message}");
                    Console.ResetColor();
                    return Task.FromResult(1);
                }
            });

            return command;
        }
        
        IDataAccessor GetDataAccessor(DirectoryInfo path) =>
            dataAccessor ??= new FileBasedDataAccessor(path, SystemClock.Instance);
            
        DateTime ParseTimeOrDuration(string token, DateTime now)
        {
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

            throw new ArgumentException($"Couldn't figure out what time \"{token}\" refers to.");
        }
    }
}