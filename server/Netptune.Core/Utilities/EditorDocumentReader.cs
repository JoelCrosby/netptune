using System.Text;
using System.Text.Json;

using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace Netptune.Core.Utilities;

public static class EditorDocumentReader
{
    private const int MaxItemDepth = 32;

    private const int DefaultHeaderLevel = 2;

    private static readonly char[] Escapable = ['\\', '`', '*', '_'];

    public static string? ToMarkdown(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var looksLikeJson = value.AsSpan().TrimStart().StartsWith("{");

        if (!looksLikeJson)
        {
            return value.Trim();
        }

        try
        {
            return ReadDocument(value);
        }
        catch (JsonException)
        {
            return value.Trim();
        }
    }

    private static string? ReadDocument(string value)
    {
        using var document = JsonDocument.Parse(value);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            return value.Trim();
        }

        var hasBlocks = root.TryGetProperty("blocks", out var blocks);

        if (!hasBlocks || blocks.ValueKind != JsonValueKind.Array)
        {
            return value.Trim();
        }

        var parser = new HtmlParser();
        var segments = new List<string>();

        foreach (var block in blocks.EnumerateArray())
        {
            var segment = ReadBlock(parser, block);

            if (string.IsNullOrWhiteSpace(segment))
            {
                continue;
            }

            segments.Add(segment);
        }

        if (segments.Count == 0)
        {
            return null;
        }

        return string.Join("\n\n", segments);
    }

    private static string? ReadBlock(HtmlParser parser, JsonElement block)
    {
        var hasData = block.TryGetProperty("data", out var data);

        if (!hasData || data.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var type = ReadString(block, "type");

        switch (type)
        {
            case "header":
                return ReadHeader(parser, data);

            case "code":
                return ReadCode(data);

            case "list":
                return ReadList(parser, data, ReadString(data, "style") ?? "unordered");

            case "checklist":
                return ReadList(parser, data, "checklist");

            case "quote":
                return ReadQuote(parser, data);

            case "delimiter":
                return "---";

            case "image":
                return ReadImage(parser, data);

            case "attaches":
                return ReadAttachment(parser, data);

            case "link":
            case "linkTool":
                return ReadString(data, "link");

            case "embed":
                return ReadString(data, "source") ?? ReadString(data, "embed");

            default:
                return ReadInline(parser, ReadString(data, "text") ?? ReadString(data, "caption"));
        }
    }

    private static string ReadHeader(HtmlParser parser, JsonElement data)
    {
        var level = ReadLevel(data);
        var text = ReadInline(parser, ReadString(data, "text"));

        return $"{new string('#', level)} {text}";
    }

    private static int ReadLevel(JsonElement data)
    {
        var hasLevel = data.TryGetProperty("level", out var value);

        if (!hasLevel)
        {
            return DefaultHeaderLevel;
        }

        var isNumber = value.TryGetInt32(out var level);

        if (!isNumber)
        {
            return DefaultHeaderLevel;
        }

        return Math.Clamp(level, 1, 6);
    }

    private static string ReadCode(JsonElement data)
    {
        var code = ReadString(data, "code") ?? string.Empty;

        return $"```\n{code}\n```";
    }

    private static string ReadQuote(HtmlParser parser, JsonElement data)
    {
        var text = ReadInline(parser, ReadString(data, "text"));
        var quoted = text.Split('\n').Select(line => $"> {line}");

        return string.Join("\n", quoted);
    }

    private static string ReadImage(HtmlParser parser, JsonElement data)
    {
        var caption = ReadInline(parser, ReadString(data, "caption"));
        var url = ReadFileUrl(data);

        return $"![{caption}]({url})";
    }

    private static string ReadAttachment(HtmlParser parser, JsonElement data)
    {
        var title = ReadInline(parser, ReadString(data, "title"));
        var url = ReadFileUrl(data);
        var hasTitle = !string.IsNullOrWhiteSpace(title);
        var label = hasTitle ? title : url;

        return $"[{label}]({url})";
    }

    private static string ReadFileUrl(JsonElement data)
    {
        var hasFile = data.TryGetProperty("file", out var file) && file.ValueKind == JsonValueKind.Object;

        if (!hasFile)
        {
            return ReadString(data, "url") ?? string.Empty;
        }

        return ReadString(file, "url") ?? string.Empty;
    }

    private static string ReadList(HtmlParser parser, JsonElement data, string style)
    {
        var lines = new List<string>();

        AppendItems(parser, lines, data, style, 0);

        return string.Join("\n", lines);
    }

    private static void AppendItems(
        HtmlParser parser,
        List<string> lines,
        JsonElement data,
        string style,
        int depth)
    {
        var hasItems = data.TryGetProperty("items", out var items);
        var canRead = hasItems && items.ValueKind == JsonValueKind.Array && depth <= MaxItemDepth;

        if (!canRead)
        {
            return;
        }

        var indent = new string(' ', depth * 2);
        var number = ReadStart(data);

        foreach (var item in items.EnumerateArray())
        {
            var content = ReadItemContent(parser, item);
            var marker = ReadMarker(item, style, number);

            lines.Add($"{indent}{marker}{content}");
            number++;

            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            AppendItems(parser, lines, item, style, depth + 1);
        }
    }

    private static string ReadItemContent(HtmlParser parser, JsonElement item)
    {
        if (item.ValueKind == JsonValueKind.String)
        {
            return ReadInline(parser, item.GetString()).Replace("\n", " ", StringComparison.Ordinal);
        }

        if (item.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        var content = ReadString(item, "content") ?? ReadString(item, "text");

        return ReadInline(parser, content).Replace("\n", " ", StringComparison.Ordinal);
    }

    private static string ReadMarker(JsonElement item, string style, int number)
    {
        var isOrdered = string.Equals(style, "ordered", StringComparison.Ordinal);

        if (isOrdered)
        {
            return $"{number}. ";
        }

        var isChecklist = string.Equals(style, "checklist", StringComparison.Ordinal);

        if (!isChecklist)
        {
            return "- ";
        }

        var isChecked = ReadChecked(item);

        return isChecked ? "- [x] " : "- [ ] ";
    }

    // The checklist tool keeps the flag on the item, the list tool keeps it in the item's meta.
    private static bool ReadChecked(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        var hasFlag = item.TryGetProperty("checked", out var flag);

        if (hasFlag)
        {
            return flag.ValueKind == JsonValueKind.True;
        }

        var hasMeta = item.TryGetProperty("meta", out var meta) && meta.ValueKind == JsonValueKind.Object;

        if (!hasMeta)
        {
            return false;
        }

        var hasChecked = meta.TryGetProperty("checked", out var value);

        return hasChecked && value.ValueKind == JsonValueKind.True;
    }

    private static int ReadStart(JsonElement data)
    {
        var hasMeta = data.TryGetProperty("meta", out var meta) && meta.ValueKind == JsonValueKind.Object;

        if (!hasMeta)
        {
            return 1;
        }

        var hasStart = meta.TryGetProperty("start", out var start);

        if (!hasStart)
        {
            return 1;
        }

        var isNumber = start.TryGetInt32(out var value);

        if (!isNumber)
        {
            return 1;
        }

        return Math.Max(value, 1);
    }

    private static string ReadInline(HtmlParser parser, string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var document = parser.ParseDocument($"<body>{html}</body>");
        var body = document.Body;

        if (body is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();

        AppendChildren(builder, body);

        return builder.ToString().Trim();
    }

    private static void AppendChildren(StringBuilder builder, INode node)
    {
        foreach (var child in node.ChildNodes)
        {
            AppendNode(builder, child);
        }
    }

    private static void AppendNode(StringBuilder builder, INode node)
    {
        if (node.NodeType == NodeType.Text)
        {
            builder.Append(Escape(node.TextContent));

            return;
        }

        if (node is not IElement element)
        {
            return;
        }

        switch (element.TagName)
        {
            case "B":
            case "STRONG":
                AppendWrapped(builder, element, "**");
                break;

            case "I":
            case "EM":
                AppendWrapped(builder, element, "*");
                break;

            case "S":
            case "DEL":
            case "STRIKE":
                AppendWrapped(builder, element, "~~");
                break;

            case "CODE":
                AppendCode(builder, element);
                break;

            case "A":
                AppendLink(builder, element);
                break;

            case "BR":
                builder.Append('\n');
                break;

            default:
                AppendChildren(builder, element);
                break;
        }
    }

    private static void AppendWrapped(StringBuilder builder, IElement element, string marker)
    {
        var inner = new StringBuilder();

        AppendChildren(inner, element);

        var isEmpty = inner.Length == 0 || string.IsNullOrWhiteSpace(inner.ToString());

        if (isEmpty)
        {
            return;
        }

        builder.Append(marker).Append(inner).Append(marker);
    }

    private static void AppendCode(StringBuilder builder, IElement element)
    {
        var text = element.TextContent;

        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        builder.Append('`').Append(text).Append('`');
    }

    private static void AppendLink(StringBuilder builder, IElement element)
    {
        var inner = new StringBuilder();

        AppendChildren(inner, element);

        var href = element.GetAttribute("href");
        var hasHref = !string.IsNullOrWhiteSpace(href);

        if (!hasHref)
        {
            builder.Append(inner);

            return;
        }

        builder.Append('[').Append(inner).Append("](").Append(href).Append(')');
    }

    private static string Escape(string text)
    {
        var builder = new StringBuilder(text.Length);

        foreach (var character in text)
        {
            if (Escapable.Contains(character))
            {
                builder.Append('\\');
            }

            builder.Append(character);
        }

        return builder.ToString();
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
}
