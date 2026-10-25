using System.Text.Json;

using Netptune.Core.Encoding;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;

namespace Netptune.Ai.Execution.Reviews;

public sealed record AiReviewReply
{
    public string? Summary { get; init; }

    public List<AiReviewReplyFinding>? Findings { get; init; }

    public static AiReviewReply? Parse(string? raw)
    {
        var json = AiImportMappingAdvisor.ExtractJson(raw);

        if (json is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<AiReviewReply>(json, JsonOptions.Default);
        }
        catch (JsonException)
        {
            return null;
        }
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
