using System.Text.RegularExpressions;

namespace Netptune.JobServer.Util;

public static class StringExtensions
{
    public static string ToKebabCase(this string input)
    {
        return Regex.Replace(input, "([a-z])([A-Z])", "$1-$2").ToLower();
    }
}