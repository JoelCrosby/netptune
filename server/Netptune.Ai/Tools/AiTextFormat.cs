using System.Text.RegularExpressions;

namespace Netptune.Ai.Tools;

public sealed record AiTextFormat
{
    public int Characters { get; init; }

    public int Lines { get; init; }

    public int ProseLines { get; init; }

    public int LongestLine { get; init; }

    public int MedianLineLength { get; init; }

    public int WrappedLines { get; init; }
}

public static partial class AiTextFormatReader
{
    public static AiTextFormat Read(string body)
    {
        var lines = body.ReplaceLineEndings("\n").Split('\n');
        var proseLengths = new List<int>();
        var wrappedLines = 0;
        var isInFence = false;
        var previousIsParagraph = false;

        foreach (var line in lines)
        {
            var isFence = FencePattern().IsMatch(line);

            if (isFence)
            {
                isInFence = !isInFence;
                previousIsParagraph = false;

                continue;
            }

            var isBlank = string.IsNullOrWhiteSpace(line);

            if (isInFence || isBlank)
            {
                previousIsParagraph = false;

                continue;
            }

            proseLengths.Add(line.TrimEnd().Length);

            var startsBlock = BlockStartPattern().IsMatch(line);
            var continuesParagraph = previousIsParagraph && !startsBlock;

            if (continuesParagraph)
            {
                wrappedLines++;
            }

            var isHeadingOrTable = HeadingOrTablePattern().IsMatch(line);
            var endsWithHardBreak = line.EndsWith("  ", StringComparison.Ordinal) || line.EndsWith('\\');

            previousIsParagraph = !isHeadingOrTable && !endsWithHardBreak;
        }

        var sortedLengths = proseLengths.Order().ToList();
        var hasProse = sortedLengths.Count > 0;
        var longestLine = sortedLengths.LastOrDefault();
        var medianLineLength = hasProse ? sortedLengths[sortedLengths.Count / 2] : 0;
        var isEmpty = body.Length == 0;

        return new AiTextFormat
        {
            Characters = body.Length,
            Lines = isEmpty ? 0 : lines.Length,
            ProseLines = proseLengths.Count,
            LongestLine = longestLine,
            MedianLineLength = medianLineLength,
            WrappedLines = wrappedLines,
        };
    }

    [GeneratedRegex(@"^[ \t]*(```|~~~)")]
    private static partial Regex FencePattern();

    [GeneratedRegex(@"^[ \t]*([-*+][ \t]|\d+[.)][ \t]|#|>|\||(-{3,}|\*{3,}|_{3,})[ \t]*$)")]
    private static partial Regex BlockStartPattern();

    [GeneratedRegex(@"^[ \t]*(#|\|)")]
    private static partial Regex HeadingOrTablePattern();
}

public static class AiTextFormatJson
{
    public const string Description =
        "The format block summarises the text's line layout, ignoring blank lines and fenced code: lines, proseLines, "
        + "longestLine, medianLineLength, and wrappedLines, the number of prose lines that continue the line "
        + "above as part of the same paragraph. A high wrappedLines with a medianLineLength near 70-100 "
        + "usually means the text was hard-wrapped.";

    public static object From(AiTextFormat format)
    {
        return new
        {
            lines = format.Lines,
            proseLines = format.ProseLines,
            longestLine = format.LongestLine,
            medianLineLength = format.MedianLineLength,
            wrappedLines = format.WrappedLines,
        };
    }
}
