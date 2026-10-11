using System.Text;
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

    // Code is left as written, the same text the parser skips. An id with no new id would point at whatever
    // has that id in the other database, so the mention is reduced to its label.
    public static string? RewriteIds(string? markdown, IReadOnlyDictionary<string, Dictionary<int, int>> newIdsByKind)
    {
        if (string.IsNullOrEmpty(markdown))
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
                lines[index] = RewriteOutsideInlineCode(lines[index], newIdsByKind);
            }
        }

        return string.Join('\n', lines);
    }

    private static string RewriteOutsideInlineCode(string line, IReadOnlyDictionary<string, Dictionary<int, int>> newIdsByKind)
    {
        var rewritten = new StringBuilder(line.Length);
        var position = 0;

        foreach (Match code in InlineCodePattern().Matches(line))
        {
            rewritten.Append(RewriteTokens(line[position..code.Index], newIdsByKind));
            rewritten.Append(code.Value);
            position = code.Index + code.Length;
        }

        rewritten.Append(RewriteTokens(line[position..], newIdsByKind));

        return rewritten.ToString();
    }

    private static string RewriteTokens(string prose, IReadOnlyDictionary<string, Dictionary<int, int>> newIdsByKind)
    {
        return IdTokenPattern().Replace(prose, match =>
        {
            var kind = match.Groups["kind"].Value.ToLowerInvariant();
            var hasId = int.TryParse(match.Groups["key"].Value, out var oldId);

            if (!hasId)
            {
                return match.Value;
            }

            var newIds = newIdsByKind.GetValueOrDefault(kind);
            var newId = newIds?.GetValueOrDefault(oldId) ?? 0;
            var isMapped = newId > 0;
            var label = match.Groups["label"].Value;

            if (!isMapped)
            {
                return label;
            }

            return label.Length > 0 ? $"[[{kind}:{newId}|{label}]]" : $"[[{kind}:{newId}]]";
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

    [GeneratedRegex(@"\[\[(?<kind>page|project|sprint|board):(?<key>[^\]|\s]+)(?:\|(?<label>[^\]]*))?\]\]", RegexOptions.IgnoreCase)]
    private static partial Regex IdTokenPattern();

    [GeneratedRegex(@"^\s*(```|~~~)")]
    private static partial Regex FencePattern();

    [GeneratedRegex(@"`[^`]*`")]
    private static partial Regex InlineCodePattern();

    [GeneratedRegex(@"^\s*(#{1,6}\s+|>\s*|[-*+]\s+(\[[ xX]\]\s+)?|\d+[.)]\s+)")]
    private static partial Regex BlockMarkerPattern();
}
