using System.Text.RegularExpressions;

using Netptune.Core.Enums;

namespace Netptune.Core.Models.Pages;

public static partial class PageReferenceParser
{
    public const int SnippetMaxLength = 200;

    public static List<PageReference> Parse(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return [];
        }

        var references = new List<PageReference>();
        var seen = new HashSet<(PageLinkTargetType, string)>();
        var insideFence = false;

        foreach (var line in markdown.Split('\n'))
        {
            var isFence = FencePattern().IsMatch(line);

            if (isFence)
            {
                insideFence = !insideFence;

                continue;
            }

            if (insideFence)
            {
                continue;
            }

            var prose = InlineCodePattern().Replace(line, string.Empty);
            var matches = TokenPattern().Matches(prose);

            if (matches.Count == 0)
            {
                continue;
            }

            var snippet = ToSnippet(prose);

            foreach (Match match in matches)
            {
                var kind = ParseKind(match.Groups["kind"].Value);
                var key = match.Groups["key"].Value.Trim();
                var normalisedKey = kind == PageLinkTargetType.User ? key : key.ToLowerInvariant();
                var isNew = seen.Add((kind, normalisedKey));

                if (isNew)
                {
                    references.Add(new PageReference(kind, normalisedKey, snippet));
                }
            }
        }

        return references;
    }

    private static PageLinkTargetType ParseKind(string kind)
    {
        return kind.ToLowerInvariant() switch
        {
            "task" => PageLinkTargetType.Task,
            "page" => PageLinkTargetType.Page,
            _ => PageLinkTargetType.User,
        };
    }

    private static string ToSnippet(string line)
    {
        var withLabels = TokenPattern().Replace(line, match =>
        {
            var label = match.Groups["label"].Value;
            var hasLabel = label.Length > 0;

            return hasLabel ? label : match.Groups["key"].Value;
        });

        var withoutMarkers = BlockMarkerPattern().Replace(withLabels, string.Empty).Trim();
        var isTooLong = withoutMarkers.Length > SnippetMaxLength;

        return isTooLong ? withoutMarkers[..(SnippetMaxLength - 1)].TrimEnd() + "…" : withoutMarkers;
    }

    [GeneratedRegex(@"\[\[(?<kind>task|page|user):(?<key>[^\]|\s]+)(?:\|(?<label>[^\]]*))?\]\]", RegexOptions.IgnoreCase)]
    private static partial Regex TokenPattern();

    [GeneratedRegex(@"^\s*(```|~~~)")]
    private static partial Regex FencePattern();

    [GeneratedRegex(@"`[^`]*`")]
    private static partial Regex InlineCodePattern();

    [GeneratedRegex(@"^\s*(#{1,6}\s+|>\s*|[-*+]\s+(\[[ xX]\]\s+)?|\d+[.)]\s+)")]
    private static partial Regex BlockMarkerPattern();
}
