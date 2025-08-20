using System;
using System.CommandLine;
using System.Reflection;
using System.Threading.Tasks;
using totlib;

namespace tot;

public static class Program
{
    static int Main(string[] args)
    {
        try
        {
            var rootCommand = tot.CommandLineParser.Create();
            return rootCommand.Parse(args).Invoke();
        }
        catch (TotException e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(e.Message);
            Console.ResetColor();
            return 1;
        }
        catch (TargetInvocationException e) when (e.InnerException is TotException)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(e.InnerException.Message);
            Console.ResetColor();
            return 1;
        }
    }
}