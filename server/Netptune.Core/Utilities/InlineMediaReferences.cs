using System.Text.RegularExpressions;

namespace Netptune.Core.Utilities;

public static partial class InlineMediaReferences
{
    // Inline media is embedded by its content url, /api/workspaces/{slug}/files/{contentId}/content.
    public static IReadOnlyList<string> ExtractContentIds(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        return ContentUrlRegex()
            .Matches(content)
            .Select(match => match.Groups[1].Value)
            .Distinct()
            .ToList();
    }

    [GeneratedRegex(@"/workspaces/[^/\s""')]+/files/([A-Za-z0-9_-]+)/content")]
    private static partial Regex ContentUrlRegex();
}
