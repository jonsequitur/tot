using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using totlib;
using TimeSpanParserUtil;

namespace tot
{
    public static class Program
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
                new [] { "-t" , "--time" },
                description: "The time to record with the event", 
                parseArgument: result =>
                {
                    var token = result.Tokens.Single().Value;

                    if (DateTime.TryParse(token, out var datetime))
                    {
                        return datetime;
                    }

                    if (TimeSpanParser.TryParse(token, out var timespan))
                    {
                        return (dataAccessor?.Clock ?? SystemClock.Instance).Now.Add(timespan);
                    }

                    result.ErrorMessage = $"Couldn't figure out what time \"{token}\" refers to.";

                    return default;
                });

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
                          .HandleExceptionsGracefully()
                          .UseDefaults();

            return builder.Build();
        }

        public static CommandLineBuilder HandleExceptionsGracefully(this CommandLineBuilder builder) =>
            builder.UseMiddleware(async (context, next) =>
            {
                try
                {
                    await next(context);
                }
                catch (TotException e)
                {
                    context.Console.Error.WriteLine(e.Message);
                    context.ResultCode = 1;
                }
                catch (TargetInvocationException e) when (e.InnerException is TotException)
                {
                    context.Console.Error.WriteLine(e.InnerException.Message);
                    context.ResultCode = 1;
                }
            }, MiddlewareOrder.ExceptionHandler);

        private static void EnsureDataAccessorIsInitialized(
            DirectoryInfo path,
            ref IDataAccessor dataAccessor) =>
            dataAccessor ??=
                new FileBasedDataAccessor(
                    path,
                    SystemClock.Instance);
    }
}