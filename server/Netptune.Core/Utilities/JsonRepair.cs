using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;

namespace Netptune.Core.Utilities;

// Generated JSON often quotes words inside a string without escaping them. A quote only closes a value when valid JSON
// can follow it, so any other quote inside a value is content. A wrong repair can still parse and quietly lose
// data, so anything that does not fit the JSON around it gives up and returns null.
public static partial class JsonRepair
{
    private const int GaveUp = -1;

    private static readonly SearchValues<char> StringDelimiters = SearchValues.Create("\"\\");

    public static string? EscapeStrayQuotes(string raw)
    {
        var start = raw.IndexOf('{');

        if (start < 0)
        {
            return null;
        }

        var repaired = new StringBuilder(raw, 0, start, raw.Length);
        var containers = new Stack<char>();
        var expectsKey = false;
        var index = start;

        while (index < raw.Length)
        {
            var character = raw[index];

            if (character is '"' || !IsStructural(character))
            {
                index = character is '"'
                    ? CopyString(raw, index, new StringContext(expectsKey, containers.Peek()), repaired)
                    : CopyLiteral(raw, index, repaired);

                expectsKey = false;

                if (index == GaveUp)
                {
                    return null;
                }

                continue;
            }

            TrackContainer(character, containers);
            repaired.Append(character);
            index++;

            if (!char.IsWhiteSpace(character))
            {
                expectsKey = character is '{' or ',';
            }

            if (containers.Count == 0)
            {
                return FinishAfterRoot(raw, index, repaired);
            }
        }

        return repaired.ToString();
    }

    private static bool IsStructural(char character)
    {
        return char.IsWhiteSpace(character) || character is '{' or '}' or '[' or ']' or ',' or ':';
    }

    private static string? FinishAfterRoot(string raw, int index, StringBuilder repaired)
    {
        var isAtEndOfJson = EndOfJson().IsMatch(raw, index);

        return isAtEndOfJson ? repaired.Append(raw, index, raw.Length - index).ToString() : null;
    }

    private static void TrackContainer(char character, Stack<char> containers)
    {
        if (character is '{' or '[')
        {
            containers.Push(character);
        }
        else if (character is '}' or ']')
        {
            containers.TryPop(out _);
        }
    }

    private static int CopyLiteral(string raw, int index, StringBuilder repaired)
    {
        var literal = LiteralToken().Match(raw, index);

        if (!literal.Success)
        {
            return GaveUp;
        }

        repaired.Append(literal.Value);

        return index + literal.Length;
    }

    private static int CopyString(string raw, int start, StringContext context, StringBuilder repaired)
    {
        repaired.Append('"');

        var isKey = context.ExpectsKey && context.Container == '{';
        var index = start + 1;

        while (index < raw.Length)
        {
            var remaining = raw.AsSpan(index);
            var runLength = remaining.IndexOfAny(StringDelimiters);

            if (runLength < 0)
            {
                repaired.Append(remaining);

                return raw.Length;
            }

            repaired.Append(remaining[..runLength]);
            index += runLength;

            if (raw[index] == '\\')
            {
                var escapeEnd = Math.Min(index + 2, raw.Length);

                repaired.Append(raw, index, escapeEnd - index);
                index = escapeEnd;

                continue;
            }

            var closesString = isKey || IsValueEnd(raw, index + 1, context.Container);

            if (closesString)
            {
                repaired.Append('"');

                return index + 1;
            }

            var closesAKey = KeyFollower().IsMatch(raw, index + 1);

            if (closesAKey)
            {
                return GaveUp;
            }

            repaired.Append("\\\"");
            index++;
        }

        return index;
    }

    private static bool IsValueEnd(string raw, int start, char container)
    {
        var follower = container == '{' ? ObjectValueFollower() : ArrayItemFollower();

        return follower.IsMatch(raw, start);
    }

    [GeneratedRegex("""\G\s*(?:\z|\}|,\s*(?:\}|"\w+"\s*:))""")]
    private static partial Regex ObjectValueFollower();

    [GeneratedRegex("""\G\s*(?:\z|\]|,\s*(?:\]|"|\{|\[|-?\d|true\b|false\b|null\b))""")]
    private static partial Regex ArrayItemFollower();

    [GeneratedRegex("""\G\s*:\s*(?:"|\{|\[|-?\d|true\b|false\b|null\b)""")]
    private static partial Regex KeyFollower();

    [GeneratedRegex("""\G(?:true\b|false\b|null\b|-?\d[\d.eE+-]*)""")]
    private static partial Regex LiteralToken();

    [GeneratedRegex("""\G[ \t]*(?:\r?\n|\z|`)""")]
    private static partial Regex EndOfJson();

    private readonly record struct StringContext(bool ExpectsKey, char Container);
}
