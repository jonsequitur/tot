using System;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Globalization;
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
    }
}