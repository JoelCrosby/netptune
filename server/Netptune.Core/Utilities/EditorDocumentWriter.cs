using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Netptune.Core.Utilities;

public static partial class EditorDocumentWriter
{
    private const string Unordered = "unordered";

    private const string Ordered = "ordered";

    private const string Checklist = "checklist";

    private const int TabWidth = 4;

    private static readonly char[] Escapable =
        ['\\', '`', '*', '_', '{', '}', '[', ']', '(', ')', '#', '+', '-', '.', '!', '|', '~', '>', '<'];

    private static readonly string[] AllowedSchemes = ["http://", "https://", "mailto:", "ftp://"];

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string? FromMarkdown(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return null;
        }

        if (IsEditorDocument(markdown))
        {
            return markdown;
        }

        var lines = SplitLines(markdown);
        var blocks = ReadBlocks(lines);

        if (blocks.Count == 0)
        {
            return null;
        }

        var written = new JsonArray();

        foreach (var block in blocks)
        {
            written.Add(block);
        }

        var document = new JsonObject
        {
            ["time"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ["blocks"] = written,
        };

        return document.ToJsonString(WriteOptions);
    }

    // A value that is already a document must survive untouched: a reviewer can hand one back by
    // editing a proposal, and reparsing it as prose would bury the blocks in escaped punctuation.
    private static bool IsEditorDocument(string value)
    {
        var looksLikeJson = value.AsSpan().TrimStart().StartsWith("{");

        if (!looksLikeJson)
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(value);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var hasBlocks = root.TryGetProperty("blocks", out var blocks);

            return hasBlocks && blocks.ValueKind == JsonValueKind.Array;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string[] SplitLines(string markdown)
    {
        return markdown.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');
    }

    private static List<JsonObject> ReadBlocks(IReadOnlyList<string> lines)
    {
        var blocks = new List<JsonObject>();
        var index = 0;

        while (index < lines.Count)
        {
            var line = lines[index];
            var isBlank = string.IsNullOrWhiteSpace(line);

            if (isBlank)
            {
                index++;

                continue;
            }

            var fence = FencePattern().Match(line);

            if (fence.Success)
            {
                index = AppendCode(blocks, lines, index, fence);

                continue;
            }

            var heading = HeadingPattern().Match(line);

            if (heading.Success)
            {
                blocks.Add(Header(heading));
                index++;

                continue;
            }

            if (IsRule(line))
            {
                index++;

                continue;
            }

            if (IsListItem(line))
            {
                index = AppendList(blocks, lines, index);

                continue;
            }

            if (QuotePattern().IsMatch(line))
            {
                index = AppendQuote(blocks, lines, index);

                continue;
            }

            index = AppendParagraph(blocks, lines, index);
        }

        return blocks;
    }

    private static JsonObject Header(Match heading)
    {
        var level = heading.Groups[1].Value.Length;
        var text = ToHtml(heading.Groups[2].Value);

        return Block("header", new JsonObject { ["text"] = text, ["level"] = level });
    }

    // The code tool holds plain text, so the body is taken verbatim minus the indentation the
    // opening fence sits at, and the info string naming a language has nowhere to go.
    private static int AppendCode(List<JsonObject> blocks, IReadOnlyList<string> lines, int start, Match fence)
    {
        var marker = fence.Groups[2].Value;
        var indent = fence.Groups[1].Value.Length;
        var body = new List<string>();
        var index = start + 1;

        while (index < lines.Count)
        {
            var line = lines[index];
            var closing = FencePattern().Match(line);
            var isClosing = closing.Success
                && closing.Groups[2].Value[0] == marker[0]
                && closing.Groups[2].Value.Length >= marker.Length
                && string.IsNullOrWhiteSpace(closing.Groups[3].Value);

            if (isClosing)
            {
                index++;

                break;
            }

            body.Add(Dedent(line, indent));
            index++;
        }

        var code = string.Join("\n", body);

        blocks.Add(Block("code", new JsonObject { ["code"] = code }));

        return index;
    }

    private static string Dedent(string line, int indent)
    {
        var removed = 0;

        while (removed < indent && removed < line.Length && line[removed] is ' ' or '\t')
        {
            removed++;
        }

        return line[removed..];
    }

    // No quote tool is registered with the editor, so a quoted passage keeps its prose and loses
    // its framing rather than showing the reader a stray "> ".
    private static int AppendQuote(List<JsonObject> blocks, IReadOnlyList<string> lines, int start)
    {
        var quoted = new List<string>();
        var index = start;

        while (index < lines.Count)
        {
            var match = QuotePattern().Match(lines[index]);

            if (!match.Success)
            {
                break;
            }

            quoted.Add(match.Groups[1].Value);
            index++;
        }

        var inner = ReadBlocks(quoted);

        blocks.AddRange(inner);

        return index;
    }

    private static int AppendParagraph(List<JsonObject> blocks, IReadOnlyList<string> lines, int start)
    {
        var parts = new List<string>();
        var index = start;

        while (index < lines.Count)
        {
            var line = lines[index];
            var isBlank = string.IsNullOrWhiteSpace(line);
            var startsNewBlock = !isBlank && StartsBlock(line);
            var ends = isBlank || (index > start && startsNewBlock);

            if (ends)
            {
                break;
            }

            parts.Add(line.Trim());
            index++;
        }

        var source = string.Join("\n", parts);
        var text = ToHtml(source).Replace("\n", "<br>", StringComparison.Ordinal);

        blocks.Add(Block("paragraph", new JsonObject { ["text"] = text }));

        return index;
    }

    private static bool StartsBlock(string line)
    {
        var isFence = FencePattern().IsMatch(line);
        var isHeading = HeadingPattern().IsMatch(line);
        var isQuote = QuotePattern().IsMatch(line);

        return isFence || isHeading || isQuote || IsRule(line) || IsListItem(line);
    }

    private static bool IsRule(string line)
    {
        return RulePattern().IsMatch(line);
    }

    private static bool IsListItem(string line)
    {
        return !IsRule(line) && ListItemPattern().IsMatch(line);
    }

    private sealed record MarkdownListItem
    {
        public int Indent { get; init; }

        public required string Text { get; init; }

        public int Number { get; init; }

        public bool? Checked { get; init; }
    }

    private sealed record ListLevel(int Indent, JsonArray Items);

    private static int AppendList(List<JsonObject> blocks, IReadOnlyList<string> lines, int start)
    {
        var items = new List<MarkdownListItem>();
        var index = ReadListItems(items, lines, start);

        if (items.Count == 0)
        {
            return start + 1;
        }

        var first = items[0];
        var style = Style(first);
        var isOrdered = string.Equals(style, Ordered, StringComparison.Ordinal);
        var meta = isOrdered
            ? new JsonObject { ["start"] = first.Number, ["counterType"] = "numeric" }
            : new JsonObject();

        var data = new JsonObject
        {
            ["style"] = style,
            ["meta"] = meta,
            ["items"] = BuildItems(items, style),
        };

        blocks.Add(Block("list", data));

        return index;
    }

    private static string Style(MarkdownListItem item)
    {
        if (item.Checked.HasValue)
        {
            return Checklist;
        }

        var isNumbered = item.Number > 0;

        return isNumbered ? Ordered : Unordered;
    }

    private static int ReadListItems(List<MarkdownListItem> items, IReadOnlyList<string> lines, int start)
    {
        var index = start;

        while (index < lines.Count)
        {
            var line = lines[index];
            var isBlank = string.IsNullOrWhiteSpace(line);

            if (isBlank)
            {
                var next = FindContent(lines, index + 1);

                if (!next.HasValue)
                {
                    return index;
                }

                var isLoose = IsListItem(lines[next.Value]);

                if (!isLoose)
                {
                    return index;
                }

                index = next.Value;

                continue;
            }

            if (IsListItem(line))
            {
                var item = ReadListItem(line);
                var isRoot = items.Count > 0 && item.Indent <= items[0].Indent;
                var changesStyle = isRoot && !string.Equals(Style(item), Style(items[0]), StringComparison.Ordinal);

                if (changesStyle)
                {
                    return index;
                }

                items.Add(item);
                index++;

                continue;
            }

            var isWrapped = items.Count > 0 && ReadIndent(line) >= 2;

            if (!isWrapped)
            {
                return index;
            }

            var wrapped = items[^1];

            items[^1] = wrapped with { Text = $"{wrapped.Text} {line.Trim()}" };
            index++;
        }

        return index;
    }

    private static int? FindContent(IReadOnlyList<string> lines, int start)
    {
        for (var index = start; index < lines.Count; index++)
        {
            var isBlank = string.IsNullOrWhiteSpace(lines[index]);

            if (!isBlank)
            {
                return index;
            }
        }

        return null;
    }

    private static MarkdownListItem ReadListItem(string line)
    {
        var match = ListItemPattern().Match(line);
        var indent = ReadIndent(line);
        var isNumbered = match.Groups[1].Success;
        var number = isNumbered ? int.Parse(match.Groups[1].Value) : 0;
        var text = match.Groups[2].Value;
        var task = TaskMarkerPattern().Match(text);

        if (!task.Success)
        {
            return new MarkdownListItem { Indent = indent, Text = text, Number = number };
        }

        var isChecked = task.Groups[1].Value is "x" or "X";

        return new MarkdownListItem
        {
            Indent = indent,
            Text = task.Groups[2].Value,
            Number = number,
            Checked = isChecked,
        };
    }

    private static int ReadIndent(string line)
    {
        var width = 0;

        foreach (var character in line)
        {
            if (character == ' ')
            {
                width++;

                continue;
            }

            if (character == '\t')
            {
                width += TabWidth;

                continue;
            }

            break;
        }

        return width;
    }

    private static JsonArray BuildItems(List<MarkdownListItem> items, string style)
    {
        var roots = new JsonArray();
        var levels = new List<ListLevel> { new(items[0].Indent, roots) };

        foreach (var item in items)
        {
            while (levels.Count > 1 && item.Indent < levels[^1].Indent)
            {
                levels.RemoveAt(levels.Count - 1);
            }

            var siblings = levels[^1].Items;
            var isNested = item.Indent > levels[^1].Indent && siblings.Count > 0;

            if (isNested)
            {
                var parent = siblings[^1]!.AsObject();

                levels.Add(new ListLevel(item.Indent, parent["items"]!.AsArray()));
            }

            levels[^1].Items.Add(ListItem(item, style));
        }

        return roots;
    }

    private static JsonObject ListItem(MarkdownListItem item, string style)
    {
        var meta = new JsonObject();
        var isChecklist = string.Equals(style, Checklist, StringComparison.Ordinal);

        if (isChecklist)
        {
            meta["checked"] = item.Checked ?? false;
        }

        return new JsonObject
        {
            ["content"] = ToHtml(item.Text),
            ["meta"] = meta,
            ["items"] = new JsonArray(),
        };
    }

    private static JsonObject Block(string type, JsonObject data)
    {
        return new JsonObject { ["type"] = type, ["data"] = data };
    }

    // Inline text reaches the editor as html, so the markers become the tags the registered inline
    // tools produce. Strikethrough has no tool behind it and keeps only its text.
    private static string ToHtml(string text)
    {
        var builder = new StringBuilder();
        var index = 0;

        while (index < text.Length)
        {
            var character = text[index];
            var escaped = character == '\\' && index + 1 < text.Length && Escapable.Contains(text[index + 1]);

            if (escaped)
            {
                AppendEncoded(builder, text[index + 1]);
                index += 2;

                continue;
            }

            var read = ReadSpan(text, index);

            if (read is not null)
            {
                builder.Append(read.Html);
                index = read.End;

                continue;
            }

            AppendEncoded(builder, character);
            index++;
        }

        return builder.ToString();
    }

    private sealed record InlineSpan(string Html, int End);

    private static InlineSpan? ReadSpan(string text, int index)
    {
        var character = text[index];

        if (character == '`')
        {
            return ReadCode(text, index);
        }

        if (character is '[' or '!')
        {
            return ReadLink(text, index);
        }

        if (character is '*' or '_')
        {
            return ReadEmphasis(text, index);
        }

        if (character == '~')
        {
            return ReadStrike(text, index);
        }

        return null;
    }

    private static InlineSpan? ReadCode(string text, int index)
    {
        var open = CountRun(text, index, '`');
        var start = index + open;
        var close = FindRun(text, start, '`', open);

        if (close < 0)
        {
            return null;
        }

        var content = text[start..close];
        var builder = new StringBuilder();

        foreach (var character in content)
        {
            AppendEncoded(builder, character);
        }

        var html = $"<code class=\"inline-code\">{builder}</code>";

        return new InlineSpan(html, close + open);
    }

    private static InlineSpan? ReadLink(string text, int index)
    {
        var isImage = text[index] == '!';
        var bracket = isImage ? index + 1 : index;
        var hasBracket = bracket < text.Length && text[bracket] == '[';

        if (!hasBracket)
        {
            return null;
        }

        var labelEnd = FindClosing(text, bracket, '[', ']');

        if (labelEnd < 0)
        {
            return null;
        }

        var hasTarget = labelEnd + 1 < text.Length && text[labelEnd + 1] == '(';

        if (!hasTarget)
        {
            return null;
        }

        var targetEnd = FindClosing(text, labelEnd + 1, '(', ')');

        if (targetEnd < 0)
        {
            return null;
        }

        var label = ToHtml(text[(bracket + 1)..labelEnd]);
        var target = text[(labelEnd + 2)..targetEnd].Trim();
        var href = ReadHref(target);

        if (href is null)
        {
            return new InlineSpan(label, targetEnd + 1);
        }

        return new InlineSpan($"<a href=\"{href}\">{label}</a>", targetEnd + 1);
    }

    private static string? ReadHref(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return null;
        }

        var isRelative = target[0] is '/' or '#';
        var isAllowed = AllowedSchemes.Any(scheme => target.StartsWith(scheme, StringComparison.OrdinalIgnoreCase));

        if (!isRelative && !isAllowed)
        {
            return null;
        }

        var builder = new StringBuilder();

        foreach (var character in target)
        {
            AppendEncoded(builder, character);
        }

        return builder.Replace("\"", "&quot;").ToString();
    }

    private static InlineSpan? ReadEmphasis(string text, int index)
    {
        var marker = text[index];
        var run = Math.Min(CountRun(text, index, marker), 2);
        var start = index + run;
        var isWordBound = marker == '_' && index > 0 && char.IsLetterOrDigit(text[index - 1]);

        if (isWordBound)
        {
            return null;
        }

        var close = FindRun(text, start, marker, run);

        if (close < 0 || close == start)
        {
            return null;
        }

        var end = close + run;
        var breaksWord = marker == '_' && end < text.Length && char.IsLetterOrDigit(text[end]);

        if (breaksWord)
        {
            return null;
        }

        var inner = ToHtml(text[start..close]);
        var tag = run == 2 ? "b" : "i";

        return new InlineSpan($"<{tag}>{inner}</{tag}>", end);
    }

    private static InlineSpan? ReadStrike(string text, int index)
    {
        var run = CountRun(text, index, '~');

        if (run < 2)
        {
            return null;
        }

        var start = index + 2;
        var close = FindRun(text, start, '~', 2);

        if (close < 0)
        {
            return null;
        }

        return new InlineSpan(ToHtml(text[start..close]), close + 2);
    }

    private static int CountRun(string text, int index, char marker)
    {
        var length = 0;

        while (index + length < text.Length && text[index + length] == marker)
        {
            length++;
        }

        return length;
    }

    private static int FindRun(string text, int start, char marker, int length)
    {
        for (var index = start; index + length <= text.Length; index++)
        {
            var isEscaped = index > 0 && text[index - 1] == '\\';

            if (isEscaped || text[index] != marker)
            {
                continue;
            }

            var run = CountRun(text, index, marker);

            if (run == length)
            {
                return index;
            }

            index += run - 1;
        }

        return -1;
    }

    private static int FindClosing(string text, int start, char open, char close)
    {
        var depth = 0;

        for (var index = start; index < text.Length; index++)
        {
            var isEscaped = index > start && text[index - 1] == '\\';

            if (isEscaped)
            {
                continue;
            }

            if (text[index] == open)
            {
                depth++;

                continue;
            }

            if (text[index] != close)
            {
                continue;
            }

            depth--;

            if (depth == 0)
            {
                return index;
            }
        }

        return -1;
    }

    private static void AppendEncoded(StringBuilder builder, char character)
    {
        switch (character)
        {
            case '&':
                builder.Append("&amp;");
                break;

            case '<':
                builder.Append("&lt;");
                break;

            case '>':
                builder.Append("&gt;");
                break;

            default:
                builder.Append(character);
                break;
        }
    }

    [GeneratedRegex(@"^ {0,3}(#{1,6})[ \t]+(.*?)[ \t]*#*[ \t]*$")]
    private static partial Regex HeadingPattern();

    [GeneratedRegex(@"^ {0,3}([-*_])[ \t]*(?:\1[ \t]*){2,}$")]
    private static partial Regex RulePattern();

    [GeneratedRegex(@"^[ \t]*(?:[-*+]|(\d{1,9})[.)])[ \t]+(.*)$")]
    private static partial Regex ListItemPattern();

    [GeneratedRegex(@"^\[([ xX])\][ \t]+(.*)$")]
    private static partial Regex TaskMarkerPattern();

    [GeneratedRegex(@"^([ \t]*)(`{3,}|~{3,})(.*)$")]
    private static partial Regex FencePattern();

    [GeneratedRegex(@"^ {0,3}>[ \t]?(.*)$")]
    private static partial Regex QuotePattern();
}
