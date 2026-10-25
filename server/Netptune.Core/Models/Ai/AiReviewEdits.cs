using System.Text.Json;

using Netptune.Core.Enums;

namespace Netptune.Core.Models.Ai;

public static class AiReviewSuggestionKinds
{
    public const string Edit = "edit";

    public const string Split = "split";

    public const string Link = "link";

    public const string Metadata = "metadata";

    public static string? Read(JsonElement suggestion)
    {
        var hasKind = suggestion.TryGetProperty("kind", out var kind);
        var isString = hasKind && kind.ValueKind == JsonValueKind.String;

        return isString ? kind.GetString() : null;
    }
}

public sealed record AiReviewSplitTask
{
    public required string Name { get; init; }

    public string? Description { get; init; }
}

public sealed record AiReviewSplitSuggestion
{
    public string Kind { get; init; } = AiReviewSuggestionKinds.Split;

    public List<AiReviewSplitTask> Tasks { get; init; } = [];
}

public sealed record AiReviewLinkSuggestion
{
    public string Kind { get; init; } = AiReviewSuggestionKinds.Link;

    public required string SystemId { get; init; }

    public int TaskId { get; init; }

    public RelationCategory Category { get; init; }
}

public sealed record AiReviewSuggestedTag
{
    public required string Name { get; init; }

    public bool IsNew { get; init; }
}

public sealed record AiReviewMetadataSuggestion
{
    public string Kind { get; init; } = AiReviewSuggestionKinds.Metadata;

    public List<AiReviewSuggestedTag> Tags { get; init; } = [];

    public decimal? EstimateValue { get; init; }

    public TaskPriority? Priority { get; init; }
}

public sealed record AiReviewEditSuggestion
{
    public string Kind { get; init; } = AiReviewSuggestionKinds.Edit;

    public required string Field { get; init; }

    public string? Find { get; init; }

    public required string Replace { get; init; }
}

public sealed record AiReviewAppliedEdit
{
    public required string Field { get; init; }

    public required string Before { get; init; }

    public required string After { get; init; }

    public bool Edited { get; init; }
}

public static class AiReviewEdits
{
    public static int CountOccurrences(string text, string passage)
    {
        var count = 0;
        var index = text.IndexOf(passage, StringComparison.Ordinal);

        while (index >= 0)
        {
            count++;
            index = text.IndexOf(passage, index + passage.Length, StringComparison.Ordinal);
        }

        return count;
    }

    // The passage has to still be there exactly once, or the edit would land somewhere the reviewer never saw.
    public static string? Apply(string currentText, string? find, string replace)
    {
        var isAppend = string.IsNullOrEmpty(find);

        if (isAppend)
        {
            var existing = currentText.TrimEnd();
            var separator = existing.Length > 0 ? "\n\n" : string.Empty;

            return $"{existing}{separator}{replace}";
        }

        var matchesOnce = CountOccurrences(currentText, find!) == 1;

        return matchesOnce ? currentText.Replace(find!, replace, StringComparison.Ordinal) : null;
    }
}
