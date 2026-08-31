using System.Net;
using System.Text.RegularExpressions;

namespace Netptune.Core.Utilities;

// Task descriptions are markdown. Indexing one verbatim would put its syntax into the search index
// alongside the prose, so the markers are stripped and only the authored text is kept.
public static partial class RichTextExtractor
{
    private const int MaxLength = 10_000;

    public static string? ToPlainText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var withoutFences = FencePattern().Replace(value, " ");
        var withoutImages = ImagePattern().Replace(withoutFences, "$1");
        var withoutLinks = LinkPattern().Replace(withoutImages, "$1");
        var withoutRules = RulePattern().Replace(withoutLinks, " ");
        var withoutPrefixes = LinePrefixPattern().Replace(withoutRules, string.Empty);
        var withoutEmphasis = EmphasisPattern().Replace(withoutPrefixes, string.Empty);
        var withoutUnderscores = UnderscoreEmphasisPattern().Replace(withoutEmphasis, string.Empty);
        var withoutEscapes = EscapePattern().Replace(withoutUnderscores, "$1");

        return Normalise(withoutEscapes);
    }

    // Inline html reaches here as markup ("a <code>div</code> element") and as entities ("&nbsp;"),
    // neither of which should end up as index terms.
    private static string? Normalise(string value)
    {
        var withoutMarkup = MarkupPattern().Replace(value, " ");
        var decoded = WebUtility.HtmlDecode(withoutMarkup);
        var text = WhitespacePattern().Replace(decoded, " ").Trim();

        if (text.Length == 0)
        {
            return null;
        }

        return Truncate(text);
    }

    private static string Truncate(string text)
    {
        if (text.Length <= MaxLength)
        {
            return text;
        }

        var lastSpace = text.LastIndexOf(' ', MaxLength);
        var end = lastSpace > 0 ? lastSpace : MaxLength;

        return text[..end];
    }

    [GeneratedRegex(@"^[ \t]*(?:`{3,}|~{3,}).*$", RegexOptions.Multiline)]
    private static partial Regex FencePattern();

    [GeneratedRegex(@"!\[([^\]]*)\]\([^)]*\)")]
    private static partial Regex ImagePattern();

    [GeneratedRegex(@"\[([^\]]*)\]\([^)]*\)")]
    private static partial Regex LinkPattern();

    [GeneratedRegex(@"^[ \t]*(?:-{3,}|\*{3,}|_{3,})[ \t]*$", RegexOptions.Multiline)]
    private static partial Regex RulePattern();

    [GeneratedRegex(
        @"^[ \t]*(?:(?:>[ \t]?)+|#{1,6}[ \t]+|(?:[-*+]|\d{1,9}[.)])[ \t]+(?:\[[ xX]\][ \t]+)?)",
        RegexOptions.Multiline)]
    private static partial Regex LinePrefixPattern();

    [GeneratedRegex(@"[*`~]")]
    private static partial Regex EmphasisPattern();

    [GeneratedRegex(@"(?<!\w)_{1,2}(?=\S)|(?<=\S)_{1,2}(?!\w)")]
    private static partial Regex UnderscoreEmphasisPattern();

    [GeneratedRegex(@"\\([\\`*_\[\]#>~+.!-])")]
    private static partial Regex EscapePattern();

    [GeneratedRegex("<[^>]*>")]
    private static partial Regex MarkupPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
