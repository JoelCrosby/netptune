using System.Text.RegularExpressions;

namespace Netptune.Core.Models.Pages;

public static partial class PageSummary
{
    public const int ExcerptSourceLength = 2_000;

    private const int ExcerptLength = 160;
    private const int ExcerptLines = 2;
    private const int WordsPerMinute = 200;

    public static int ReadMinutes(long wordCount)
    {
        var minutes = Math.Round(wordCount / (double)WordsPerMinute, MidpointRounding.AwayFromZero);

        return Math.Max(1, (int)minutes);
    }

    public static string? Excerpt(string? bodyPrefix, bool isTruncated)
    {
        if (string.IsNullOrWhiteSpace(bodyPrefix))
        {
            return null;
        }

        var lines = bodyPrefix.Split('\n');
        var lastLineIsCut = isTruncated && lines.Length > 1;
        var completeLines = lastLineIsCut ? lines[..^1] : lines;
        var completeProse = ProseLines(completeLines).Take(ExcerptLines).ToList();
        var prose = completeProse.Count > 0 ? completeProse : ProseLines(lines).Take(ExcerptLines).ToList();
        var text = string.Join(' ', prose);

        if (text.Length == 0)
        {
            return null;
        }

        return Shorten(text);
    }

    private static IEnumerable<string> ProseLines(IEnumerable<string> lines)
    {
        var inFence = false;

        foreach (var line in lines)
        {
            var isFenceLine = FencePattern().IsMatch(line);

            if (isFenceLine)
            {
                inFence = !inFence;
                continue;
            }

            var isHeading = HeadingPattern().IsMatch(line);
            var isProse = !inFence && !isHeading;

            if (!isProse)
            {
                continue;
            }

            var text = PageSearchText.ToPlainText(line);

            if (text is not null)
            {
                yield return text;
            }
        }
    }

    private static string Shorten(string text)
    {
        if (text.Length <= ExcerptLength)
        {
            return text;
        }

        var lastSpace = text.LastIndexOf(' ', ExcerptLength);
        var end = lastSpace > 0 ? lastSpace : ExcerptLength;
        var shortened = text[..end].TrimEnd(' ', ',', ';', ':', '.', '-');

        return $"{shortened}…";
    }

    [GeneratedRegex(@"^\s*(```|~~~)")]
    private static partial Regex FencePattern();

    [GeneratedRegex(@"^[ ]{0,3}#{1,6}(\s|$)")]
    private static partial Regex HeadingPattern();
}
