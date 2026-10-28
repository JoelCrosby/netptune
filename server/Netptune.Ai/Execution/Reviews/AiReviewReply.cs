using System.Text.Json;

using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;

namespace Netptune.Ai.Execution.Reviews;

public sealed record AiReviewReply
{
    private const int MaxUnclosedBraces = 16;

    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    public string? Summary { get; init; }

    public List<AiReviewReplyFinding>? Findings { get; init; }

    public static AiReviewReplyParse Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return AiReviewReplyParse.Failed("The reply was empty.");
        }

        var problem = "The reply contained no complete JSON object.";

        foreach (var candidate in ObjectCandidates(raw))
        {
            try
            {
                using var document = JsonDocument.Parse(candidate, DocumentOptions);
                var reply = Read(document.RootElement);

                if (reply is not null)
                {
                    return AiReviewReplyParse.Succeeded(reply);
                }

                problem = "The JSON object had no summary or findings.";
            }
            catch (JsonException exception)
            {
                problem = exception.Message;
            }
        }

        return AiReviewReplyParse.Failed(problem);
    }

    // Models fence their JSON or wrap it in prose, and that prose can hold braces of its own, so each
    // top-level balanced object is tried in turn rather than everything between the first { and the last }.
    // An unclosed brace scans to the end of the reply, so retries past one are capped to keep long replies cheap.
    private static IEnumerable<string> ObjectCandidates(string raw)
    {
        var start = raw.IndexOf('{');
        var unclosedBraces = 0;

        while (start >= 0)
        {
            var end = FindObjectEnd(raw, start);

            if (end < 0)
            {
                unclosedBraces++;

                var hasRetriesLeft = unclosedBraces < MaxUnclosedBraces;

                if (!hasRetriesLeft)
                {
                    yield break;
                }

                start = raw.IndexOf('{', start + 1);

                continue;
            }

            yield return raw[start..(end + 1)];

            start = raw.IndexOf('{', end + 1);
        }
    }

    private static int FindObjectEnd(string raw, int start)
    {
        var depth = 0;
        var inString = false;
        var isEscaped = false;

        for (var index = start; index < raw.Length; index++)
        {
            var character = raw[index];

            if (inString)
            {
                if (isEscaped)
                {
                    isEscaped = false;
                }
                else if (character == '\\')
                {
                    isEscaped = true;
                }
                else if (character == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (character == '"')
            {
                inString = true;
            }
            else if (character == '{')
            {
                depth++;
            }
            else if (character == '}')
            {
                depth--;

                if (depth == 0)
                {
                    return index;
                }
            }
        }

        return -1;
    }

    private static AiReviewReply? Read(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var summary = Property(root, "summary");
        var findings = Property(root, "findings");
        var hasReviewShape = summary is not null || findings is not null;

        if (!hasReviewShape)
        {
            return null;
        }

        var readFindings = ArrayItems(findings)
            .Where(item => item.ValueKind == JsonValueKind.Object)
            .Select(ReadFinding)
            .ToList();

        return new AiReviewReply
        {
            Summary = StringValue(summary),
            Findings = readFindings,
        };
    }

    private static AiReviewReplyFinding ReadFinding(JsonElement finding)
    {
        var suggestion = Property(finding, "suggestion");
        var isSuggestionObject = suggestion?.ValueKind == JsonValueKind.Object;
        var relatedTasks = ArrayItems(Property(finding, "relatedTasks"))
            .Select(ScalarText)
            .OfType<string>()
            .ToList();

        return new AiReviewReplyFinding
        {
            Category = StringValue(Property(finding, "category")),
            Title = StringValue(Property(finding, "title")),
            Explanation = StringValue(Property(finding, "explanation")),
            Quote = StringValue(Property(finding, "quote")),
            Suggestion = isSuggestionObject ? suggestion!.Value.Clone() : null,
            RelatedTasks = relatedTasks,
        };
    }

    private static JsonElement? Property(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
        {
            var isMatch = string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase);

            if (isMatch)
            {
                return property.Value;
            }
        }

        return null;
    }

    private static IEnumerable<JsonElement> ArrayItems(JsonElement? element)
    {
        var isArray = element?.ValueKind == JsonValueKind.Array;

        return isArray ? element!.Value.EnumerateArray() : [];
    }

    private static string? StringValue(JsonElement? element)
    {
        var isString = element?.ValueKind == JsonValueKind.String;

        return isString ? element!.Value.GetString() : null;
    }

    private static string? ScalarText(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            _ => null,
        };
    }
}

public sealed record AiReviewReplyParse
{
    public AiReviewReply? Reply { get; init; }

    public string? Problem { get; init; }

    public static AiReviewReplyParse Succeeded(AiReviewReply reply)
    {
        return new AiReviewReplyParse { Reply = reply };
    }

    public static AiReviewReplyParse Failed(string problem)
    {
        return new AiReviewReplyParse { Problem = problem };
    }
}

public sealed record AiReviewReplyFinding
{
    public string? Category { get; init; }

    public string? Title { get; init; }

    public string? Explanation { get; init; }

    public string? Quote { get; init; }

    public JsonElement? Suggestion { get; init; }

    public List<string>? RelatedTasks { get; init; }
}

public sealed record AiReviewDraftFinding
{
    public AiReviewFindingCategory Category { get; init; }

    public required string Title { get; init; }

    public required string Explanation { get; init; }

    public string? Quote { get; init; }

    public JsonDocument? Suggestion { get; init; }

    public List<int> RelatedTaskIds { get; init; } = [];
}

public sealed record AiReviewDraft
{
    public string? Summary { get; init; }

    public List<AiReviewDraftFinding> Findings { get; init; } = [];
}
