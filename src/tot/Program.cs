using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Threading.Tasks;
using totlib;

namespace tot;

public static class Program
{
    static async Task<int> Main(string[] args)
    {
        return await CommandLineParser.InvokeAsync(args);
    }

    public static Parser CommandLineParser { get; } = tot.CommandLineParser.Create();

    public static CommandLineBuilder DisplayException(this CommandLineBuilder builder) =>
        builder.AddMiddleware(async (context, next) =>
        {
            try
            {
                await next(context);
            }
            catch (TotException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                context.Console.Error.WriteLine(e.Message);
                context.ExitCode = 1;
            }
            catch (TargetInvocationException e) when (e.InnerException is TotException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                context.Console.Error.WriteLine(e.InnerException.Message);
                context.ExitCode = 1;
            }
            finally
            {
                Console.ResetColor();
            }
        }, MiddlewareOrder.ExceptionHandler);
}