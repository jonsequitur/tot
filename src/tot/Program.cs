using System;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TimeSpanParserUtil;
using totlib;

namespace tot
{
    public static class Program
    {
        static async Task<int> Main(string[] args)
        {
            return await CommandLineParser.InvokeAsync(args);
        }

        public static Parser CommandLineParser { get; } = tot.CommandLineParser.Create();

        internal static ParseArgument<DateTime> ParseTimeArgument(IDataAccessor dataAccessor)
        {
            return result =>
            {
                var token = result.Tokens.SingleOrDefault()?.Value;

                var now = (dataAccessor?.Clock ?? SystemClock.Instance).Now;

                if (token == null)
                {
                    return now;
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

        public static CommandLineBuilder DisplayException(this CommandLineBuilder builder) =>
            builder.UseMiddleware(async (context, next) =>
            {
                try
                {
                    await next(context);
                }
                catch (TotException e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    context.Console.Error.WriteLine(e.Message);
                    context.ResultCode = 1;
                }
                catch (TargetInvocationException e) when (e.InnerException is TotException)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    context.Console.Error.WriteLine(e.InnerException.Message);
                    context.ResultCode = 1;
                }
                finally
                {
                    Console.ResetColor();
                }
            }, MiddlewareOrder.ExceptionHandler);

        internal static void EnsureDataAccessorIsInitialized(
            DirectoryInfo path,
            ref IDataAccessor dataAccessor) =>
            dataAccessor ??=
                new FileBasedDataAccessor(
                    path,
                    SystemClock.Instance);
    }
}