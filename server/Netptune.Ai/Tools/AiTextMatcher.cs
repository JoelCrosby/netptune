using System.Text.RegularExpressions;

namespace Netptune.Ai.Tools;

public sealed record AiTextMatchLine(int Line, string Text);

public sealed record AiTextMatches(int Count, List<AiTextMatchLine> Shown);

public static class AiTextMatcher
{
    public const string RegexDescription =
        "The database runs regular expressions with PostgreSQL syntax, in multiline mode so ^ and $ match at line "
        + "breaks; stick to what PostgreSQL and .NET share: no named groups or inline options, and no \\b, "
        + "which PostgreSQL reads as a backspace. ";

    public const string TimeoutMessage = "The pattern took too long to run. Try a simpler or more specific pattern.";

    private const int MaximumLineLength = 240;

    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    public static Regex? Create(string pattern, bool isRegex, bool isCaseSensitive)
    {
        var caseOption = isCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
        var options = RegexOptions.Multiline | RegexOptions.CultureInvariant | caseOption;
        var expression = isRegex ? pattern : Regex.Escape(pattern);

        try
        {
            return new Regex(expression, options, MatchTimeout);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    public static string InvalidMessage(string pattern)
    {
        return $"The pattern is not a valid regular expression: {pattern}";
    }

    public static AiTextMatches Find(Regex matcher, string text, int take)
    {
        var matches = matcher.Matches(text);

        if (matches.Count == 0)
        {
            return new AiTextMatches(0, []);
        }

        var lineStarts = LineStarts(text);
        var shown = matches
            .Take(take)
            .Select(match => DescribeMatch(text, lineStarts, match))
            .ToList();

        return new AiTextMatches(matches.Count, shown);
    }

    private static List<int> LineStarts(string text)
    {
        var starts = new List<int> { 0 };

        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] == '\n')
            {
                starts.Add(index + 1);
            }
        }

        return starts;
    }

    private static AiTextMatchLine DescribeMatch(string text, List<int> lineStarts, Match match)
    {
        var searchIndex = lineStarts.BinarySearch(match.Index);
        var startsALine = searchIndex >= 0;
        var line = startsALine ? searchIndex : ~searchIndex - 1;
        var start = lineStarts[line];
        var hasNextLine = line + 1 < lineStarts.Count;
        var end = hasNextLine ? lineStarts[line + 1] : text.Length;
        var lineText = text[start..end].TrimEnd('\r', '\n');
        var column = match.Index - start;

        return new AiTextMatchLine(line + 1, Excerpt(lineText, column));
    }

    private static string Excerpt(string text, int column)
    {
        var isLong = text.Length > MaximumLineLength;

        if (!isLong)
        {
            return text;
        }

        var latestStart = text.Length - MaximumLineLength;
        var windowStart = Math.Clamp(column - (MaximumLineLength / 3), 0, latestStart);
        var window = text.Substring(windowStart, MaximumLineLength);
        var prefix = windowStart > 0 ? "…" : string.Empty;
        var suffix = windowStart < latestStart ? "…" : string.Empty;

        return prefix + window + suffix;
    }
}
