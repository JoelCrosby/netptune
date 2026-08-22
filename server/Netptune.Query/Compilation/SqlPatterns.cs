namespace Netptune.Query.Compilation;

// Escapes LIKE wildcards before they reach the pattern, so someone searching for "50%" is looking
// for a literal percent sign rather than matching everything.
internal static class SqlPatterns
{
    public const string LikeEscape = @" ESCAPE '\'";

    public static string Contains(string value)
    {
        return $"%{Escape(value)}%";
    }

    public static string StartsWith(string value)
    {
        return $"{Escape(value)}%";
    }

    private static string Escape(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");
    }
}
