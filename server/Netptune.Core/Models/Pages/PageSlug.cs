using Netptune.Core.Encoding;
using Netptune.Core.Utilities;

namespace Netptune.Core.Models.Pages;

// A slug is readable words then a fixed id. Only the id identifies the page, so the words follow the title
// and a link holding older words still opens it.
public static class PageSlug
{
    private const int IdLength = 12;
    private const int WordsMaxLength = PageTree.SlugMaxLength - IdLength - 1;
    private const string FallbackWords = "untitled";

    public static string Create(string title, string? body)
    {
        var id = UniqueIdBuilder.Generate();

        return Compose(title, body, id);
    }

    public static string Follow(string slug, string title, string? body)
    {
        var id = IdOf(slug);

        if (id is null)
        {
            return slug;
        }

        return Compose(title, body, id);
    }

    public static string? IdOf(string slug)
    {
        var id = slug[(slug.LastIndexOf('-') + 1)..];
        var isId = id.Length == IdLength && id.All(char.IsAsciiLetterOrDigit);

        return isId ? id : null;
    }

    private static string Compose(string title, string? body, string id)
    {
        var words = WordsFor(title, body);

        return $"{words}-{id}";
    }

    private static string WordsFor(string title, string? body)
    {
        var hasTitle = title != PageTitle.Untitled;
        var source = hasTitle ? title : FirstLine(body) ?? title;
        var words = source.ToUrlSlug(false, WordsMaxLength);

        return words.Length > 0 ? words : FallbackWords;
    }

    private static string? FirstLine(string? body)
    {
        var lines = body?.Split('\n') ?? [];

        return lines
            .Select(line => PageSearchText.ToPlainText(line)?.Trim())
            .FirstOrDefault(text => !string.IsNullOrEmpty(text));
    }
}
