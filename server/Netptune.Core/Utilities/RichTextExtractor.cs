using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Netptune.Core.Utilities;

// Task descriptions arrive in two shapes: an editor.js document ({"blocks":[...]}) from the client,
// and bare text from API clients and from before the editor existed. Indexing the editor payload
// verbatim would put block ids, tool names and version stamps into the search index alongside the
// prose, so the document is walked and only the authored text is kept.
public static partial class RichTextExtractor
{
    private const int MaxLength = 10_000;

    private const int MaxItemDepth = 32;

    public static string? ToPlainText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var blockText = ReadBlockText(value);

        return Normalise(blockText ?? value);
    }

    private static string? ReadBlockText(string value)
    {
        var looksLikeJson = value.AsSpan().TrimStart().StartsWith("{");

        if (!looksLikeJson)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(value);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var hasBlocks = root.TryGetProperty("blocks", out var blocks);

            if (!hasBlocks || blocks.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var builder = new StringBuilder();

            foreach (var block in blocks.EnumerateArray())
            {
                AppendBlock(builder, block);
            }

            return builder.ToString();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void AppendBlock(StringBuilder builder, JsonElement block)
    {
        var hasData = block.TryGetProperty("data", out var data);

        if (!hasData || data.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var type = ReadString(block, "type");

        switch (type)
        {
            case "list":
            case "checklist":
                AppendItems(builder, data, depth: 0);
                break;

            case "code":
                Append(builder, ReadString(data, "code"));
                break;

            case "link":
                AppendLink(builder, data);
                break;

            case "image":
                Append(builder, ReadString(data, "caption"));
                break;

            case "attaches":
                AppendAttachment(builder, data);
                break;

            default:
                Append(builder, ReadString(data, "text"));
                Append(builder, ReadString(data, "caption"));
                break;
        }
    }

    private static void AppendItems(StringBuilder builder, JsonElement data, int depth)
    {
        var hasItems = data.TryGetProperty("items", out var items);
        var canRead = hasItems && items.ValueKind == JsonValueKind.Array && depth <= MaxItemDepth;

        if (!canRead)
        {
            return;
        }

        foreach (var item in items.EnumerateArray())
        {
            AppendItem(builder, item, depth);
        }
    }

    // list items are bare strings in the tool's v1 output and objects carrying nested items in v2;
    // checklist items are objects keyed on "text"
    private static void AppendItem(StringBuilder builder, JsonElement item, int depth)
    {
        if (item.ValueKind == JsonValueKind.String)
        {
            Append(builder, item.GetString());

            return;
        }

        if (item.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        Append(builder, ReadString(item, "content"));
        Append(builder, ReadString(item, "text"));

        AppendItems(builder, item, depth + 1);
    }

    private static void AppendLink(StringBuilder builder, JsonElement data)
    {
        Append(builder, ReadString(data, "link"));

        var hasMeta = data.TryGetProperty("meta", out var meta);

        if (!hasMeta || meta.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        Append(builder, ReadString(meta, "title"));
        Append(builder, ReadString(meta, "description"));
    }

    // the attaches tool seeds its title from the file name, so the two are the same string unless the
    // author renamed the attachment
    private static void AppendAttachment(StringBuilder builder, JsonElement data)
    {
        var title = ReadString(data, "title");

        if (!string.IsNullOrWhiteSpace(title))
        {
            Append(builder, title);

            return;
        }

        var hasFile = data.TryGetProperty("file", out var file);

        if (!hasFile || file.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        Append(builder, ReadString(file, "name"));
    }

    private static void Append(StringBuilder builder, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append(' ');
        }

        builder.Append(value);
    }

    private static string? ReadString(JsonElement element, string property)
    {
        var hasProperty = element.TryGetProperty(property, out var value);

        if (!hasProperty || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return value.GetString();
    }

    // inline formatting reaches here as markup ("a <code>div</code> element") and as entities
    // ("&nbsp;"), neither of which should end up as index terms
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

    [GeneratedRegex("<[^>]*>")]
    private static partial Regex MarkupPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
