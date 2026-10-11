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

    // Page ids are only meaningful inside one database, so a page carried to another one has its [[page:N]]
    // tokens moved to the new ids. Code is left as written, the same text the parser skips.
    public static string? RewritePageIds(string? markdown, IReadOnlyDictionary<int, int> newIds)
    {
        if (string.IsNullOrEmpty(markdown) || newIds.Count == 0)
        {
            return markdown;
        }

        var lines = markdown.Split('\n');
        var insideFence = false;

        for (var index = 0; index < lines.Length; index++)
        {
            var isFence = FencePattern().IsMatch(lines[index]);

            if (isFence)
            {
                insideFence = !insideFence;

                continue;
            }

            if (!insideFence)
            {
                lines[index] = RewriteOutsideInlineCode(lines[index], newIds);
            }
        }

        return string.Join('\n', lines);
    }

    private static string RewriteOutsideInlineCode(string line, IReadOnlyDictionary<int, int> newIds)
    {
        var rewritten = new System.Text.StringBuilder(line.Length);
        var position = 0;

        foreach (Match code in InlineCodePattern().Matches(line))
        {
            rewritten.Append(RewriteTokens(line[position..code.Index], newIds));
            rewritten.Append(code.Value);
            position = code.Index + code.Length;
        }

        rewritten.Append(RewriteTokens(line[position..], newIds));

        return rewritten.ToString();
    }

    private static string RewriteTokens(string prose, IReadOnlyDictionary<int, int> newIds)
    {
        return TokenPattern().Replace(prose, match =>
        {
            var kind = ParseKind(match.Groups["kind"].Value);
            var isPage = kind == PageLinkTargetType.Page;
            var hasId = int.TryParse(match.Groups["key"].Value, out var oldId);
            var newId = isPage && hasId ? newIds.GetValueOrDefault(oldId) : 0;
            var isMapped = newId > 0;

            if (!isMapped)
            {
                return match.Value;
            }

            var label = match.Groups["label"];

            return label.Success ? $"[[page:{newId}|{label.Value}]]" : $"[[page:{newId}]]";
        });
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
