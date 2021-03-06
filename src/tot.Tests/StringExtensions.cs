using static System.Environment;

namespace tot.Tests
{
    public static class StringExtensions
    {
        public static string NormalizeLineEndings(this string value) => 
            value.Replace("\r\n", "\n")
                 .Replace("\n", NewLine);
    }
}