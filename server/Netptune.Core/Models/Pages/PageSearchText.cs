using System.Text.RegularExpressions;

using Netptune.Core.Utilities;

namespace Netptune.Core.Models.Pages;

public static partial class PageSearchText
{
    public static string? ToPlainText(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        var lines = body.Split('\n');
        var inFence = false;

        for (var index = 0; index < lines.Length; index++)
        {
            var isFenceLine = FencePattern().IsMatch(lines[index]);

            if (isFenceLine)
            {
                inFence = !inFence;
                continue;
            }

            if (!inFence)
            {
                lines[index] = RewriteProse(lines[index]);
            }
        }

        return RichTextExtractor.ToPlainText(string.Join('\n', lines));
    }

    private static string RewriteProse(string line)
    {
        var isEmbed = EmbedPattern().IsMatch(line);

        if (isEmbed)
        {
            return string.Empty;
        }

        var withoutCallout = CalloutMarkerPattern().Replace(line, "$1");
        var withLabels = ReferenceOrCodePattern().Replace(withoutCallout, ReferenceLabel);

        // The editor escapes a lone {{query:…}} written as prose; the reader sees it without the backslash.
        return withLabels.Replace("\\{{", "{{");
    }

    private static string ReferenceLabel(Match match)
    {
        var isInlineCode = match.Groups["code"].Success;

        if (isInlineCode)
        {
            return match.Value;
        }

        var label = match.Groups["label"].Value.Trim();
        var isUser = match.Groups["kind"].Value.Equals("user", StringComparison.OrdinalIgnoreCase);

        if (label.Length > 0)
        {
            return label;
        }

        // A user mention without a label holds only an opaque user id, which no one searches for.
        return isUser ? string.Empty : match.Groups["key"].Value;
    }

    [GeneratedRegex(@"^\s*(```|~~~)")]
    private static partial Regex FencePattern();

    [GeneratedRegex(@"(?<code>`[^`]*`)|\[\[(?<kind>task|page|user):(?<key>[^\]|\s]+)(?:\|(?<label>[^\]]*))?\]\]", RegexOptions.IgnoreCase)]
    private static partial Regex ReferenceOrCodePattern();

    [GeneratedRegex(@"^[ ]{0,3}\{\{query:[a-zA-Z0-9-]+\}\}[ \t]*\r?$")]
    private static partial Regex EmbedPattern();

    [GeneratedRegex(@"^([ \t]*>[ \t]*)\[!(?:NOTE|INFO|TIP|IMPORTANT|WARNING|CAUTION)\][ \t]*", RegexOptions.IgnoreCase)]
    private static partial Regex CalloutMarkerPattern();
}
