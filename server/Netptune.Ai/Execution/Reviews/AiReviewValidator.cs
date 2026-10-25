using System.Text.Json;

using Netptune.Core.Encoding;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Tags;

namespace Netptune.Ai.Execution.Reviews;

public static class AiReviewValidator
{
    public const int MaxFindings = 12;
    public const int MaxTitleLength = 120;
    public const int MaxExplanationLength = 1_000;
    public const int MaxQuoteLength = 500;
    public const int MaxSummaryLength = 1_000;
    public const int MaxReplacementLength = 8_000;
    public const int MaxSplitTasks = 6;
    public const int MaxSplitNameLength = 200;
    public const int MaxSplitDescriptionLength = 4_000;
    public const int MaxNewTags = 3;
    public const int MaxTagLength = 128;
    public const decimal MaxEstimate = 1_000;

    private static readonly Dictionary<string, AiReviewFindingCategory> CategoriesByName =
        Enum.GetValues<AiReviewFindingCategory>()
            .ToDictionary(category => category.ToString(), StringComparer.OrdinalIgnoreCase);

    public static AiReviewDraft Validate(
        AiReviewReply reply,
        AiReviewContext context,
        IReadOnlySet<AiReviewFindingCategory> allowedCategories)
    {
        var findings = (reply.Findings ?? [])
            .Select(finding => ValidateFinding(finding, context, allowedCategories))
            .OfType<AiReviewDraftFinding>()
            .Take(MaxFindings)
            .ToList();

        var summary = Clean(reply.Summary, MaxSummaryLength);

        return new AiReviewDraft
        {
            Summary = summary,
            Findings = findings,
        };
    }

    private static AiReviewDraftFinding? ValidateFinding(
        AiReviewReplyFinding finding,
        AiReviewContext context,
        IReadOnlySet<AiReviewFindingCategory> allowedCategories)
    {
        var isKnownCategory = CategoriesByName.TryGetValue(finding.Category ?? string.Empty, out var category);
        var isAllowedCategory = isKnownCategory && allowedCategories.Contains(category);
        var title = Clean(finding.Title, MaxTitleLength);
        var hasTitle = title is not null;

        if (!isAllowedCategory || !hasTitle)
        {
            return null;
        }

        var relatedTaskIds = (finding.RelatedTasks ?? [])
            .Select(systemId => context.CandidateTasks.GetValueOrDefault(systemId.Trim()))
            .Where(taskId => taskId > 0)
            .Distinct()
            .ToList();

        return new AiReviewDraftFinding
        {
            Category = category,
            Title = title!,
            Explanation = Clean(finding.Explanation, MaxExplanationLength) ?? string.Empty,
            Quote = Clean(finding.Quote, MaxQuoteLength),
            Suggestion = ValidateSuggestion(finding.Suggestion, context),
            RelatedTaskIds = relatedTaskIds,
        };
    }

    private static JsonDocument? ValidateSuggestion(JsonElement? suggestion, AiReviewContext context)
    {
        var isObject = suggestion is { ValueKind: JsonValueKind.Object };

        if (!isObject)
        {
            return null;
        }

        var kind = ReadString(suggestion!.Value, "kind")?.ToLowerInvariant();

        return kind switch
        {
            AiReviewSuggestionKinds.Edit => ValidateEdit(suggestion.Value, context),
            AiReviewSuggestionKinds.Split => ValidateSplit(suggestion.Value),
            AiReviewSuggestionKinds.Link => ValidateLink(suggestion.Value, context),
            AiReviewSuggestionKinds.Metadata => ValidateMetadata(suggestion.Value, context),
            _ => null,
        };
    }

    private static JsonDocument? ValidateSplit(JsonElement suggestion)
    {
        var hasTasks = suggestion.TryGetProperty("tasks", out var tasks) && tasks.ValueKind == JsonValueKind.Array;

        if (!hasTasks)
        {
            return null;
        }

        var splitTasks = tasks.EnumerateArray()
            .Where(task => task.ValueKind == JsonValueKind.Object)
            .Select(task => new
            {
                Name = Clean(ReadString(task, "name"), MaxSplitNameLength),
                Description = Clean(ReadString(task, "description"), MaxSplitDescriptionLength),
            })
            .Where(task => task.Name is not null)
            .Select(task => new AiReviewSplitTask { Name = task.Name!, Description = task.Description })
            .Take(MaxSplitTasks)
            .ToList();

        var isWorthSplitting = splitTasks.Count >= 2;

        if (!isWorthSplitting)
        {
            return null;
        }

        var split = new AiReviewSplitSuggestion { Tasks = splitTasks };

        return JsonSerializer.SerializeToDocument(split, JsonOptions.Default);
    }

    private static JsonDocument? ValidateLink(JsonElement suggestion, AiReviewContext context)
    {
        var systemId = ReadString(suggestion, "systemId")?.Trim() ?? string.Empty;
        var category = ReadString(suggestion, "category")?.ToLowerInvariant();
        var isCandidate = context.CandidateTasks.TryGetValue(systemId, out var taskId);
        var relationCategory = category switch
        {
            "duplicate" => RelationCategory.Duplicate,
            "related" => RelationCategory.Related,
            _ => (RelationCategory?)null,
        };

        if (!isCandidate || relationCategory is null)
        {
            return null;
        }

        var link = new AiReviewLinkSuggestion
        {
            SystemId = systemId,
            TaskId = taskId,
            Category = relationCategory.Value,
        };

        return JsonSerializer.SerializeToDocument(link, JsonOptions.Default);
    }

    private static JsonDocument? ValidateMetadata(JsonElement suggestion, AiReviewContext context)
    {
        var tags = ValidateTags(suggestion, context.WorkspaceTags);
        var estimate = ReadEstimate(suggestion);
        var priority = ReadPriority(suggestion);
        var hasAnything = tags.Count > 0 || estimate is not null || priority is not null;

        if (!hasAnything)
        {
            return null;
        }

        var metadata = new AiReviewMetadataSuggestion
        {
            Tags = tags,
            EstimateValue = estimate,
            Priority = priority,
        };

        return JsonSerializer.SerializeToDocument(metadata, JsonOptions.Default);
    }

    // The tags table is unique on the exact name, so matching ignores case here or a suggestion could add
    // "bug" beside "Bug".
    private static List<AiReviewSuggestedTag> ValidateTags(JsonElement suggestion, IReadOnlyList<string> workspaceTags)
    {
        var hasTags = suggestion.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Array;

        if (!hasTags)
        {
            return [];
        }

        var existingByName = workspaceTags
            .GroupBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var suggested = new List<AiReviewSuggestedTag>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var newCount = 0;

        foreach (var tag in tags.EnumerateArray())
        {
            var raw = tag.ValueKind == JsonValueKind.String ? tag.GetString() : null;
            var name = string.IsNullOrWhiteSpace(raw) ? null : TagNames.Normalize(raw);
            var isUsable = name is not null && name.Length <= MaxTagLength && seen.Add(name);

            if (!isUsable)
            {
                continue;
            }

            var isExisting = existingByName.TryGetValue(name!, out var existingName);

            if (isExisting)
            {
                suggested.Add(new AiReviewSuggestedTag { Name = existingName!, IsNew = false });

                continue;
            }

            var hasRoomForNewTag = newCount < MaxNewTags;

            if (hasRoomForNewTag)
            {
                newCount++;
                suggested.Add(new AiReviewSuggestedTag { Name = name!, IsNew = true });
            }
        }

        return suggested;
    }

    private static decimal? ReadEstimate(JsonElement suggestion)
    {
        var hasEstimate = suggestion.TryGetProperty("estimateValue", out var estimate);
        var isNumber = hasEstimate && estimate.ValueKind == JsonValueKind.Number;
        var value = isNumber && estimate.TryGetDecimal(out var parsed) ? parsed : (decimal?)null;
        var isInRange = value is >= 0 and <= MaxEstimate;

        return isInRange ? Math.Round(value!.Value, 2) : null;
    }

    private static TaskPriority? ReadPriority(JsonElement suggestion)
    {
        var name = ReadString(suggestion, "priority");
        var isKnown = Enum.TryParse<TaskPriority>(name, true, out var priority);
        var isSet = isKnown && priority != TaskPriority.None && Enum.IsDefined(priority);

        return isSet ? priority : null;
    }

    private static JsonDocument? ValidateEdit(JsonElement suggestion, AiReviewContext context)
    {
        var field = ReadString(suggestion, "field") ?? string.Empty;
        var find = ReadString(suggestion, "find");
        var replace = ReadString(suggestion, "replace") ?? string.Empty;
        var hasField = context.EditableFields.TryGetValue(field, out var currentText);
        var isWithinLimit = replace.Length <= MaxReplacementLength;
        var isAppend = string.IsNullOrEmpty(find);
        var matchesOnce = !isAppend && hasField && AiReviewEdits.CountOccurrences(currentText!, find!) == 1;
        var changesSomething = isAppend ? replace.Trim().Length > 0 : replace != find;
        var isUsable = hasField && isWithinLimit && changesSomething && (isAppend || matchesOnce);

        if (!isUsable)
        {
            return null;
        }

        var edit = new AiReviewEditSuggestion
        {
            Field = field,
            Find = isAppend ? null : find,
            Replace = replace,
        };

        return JsonSerializer.SerializeToDocument(edit, JsonOptions.Default);
    }

    private static string? Clean(string? value, int maxLength)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return AiReviewPrompt.Truncate(trimmed, maxLength);
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        var hasProperty = element.TryGetProperty(propertyName, out var value);
        var isString = hasProperty && value.ValueKind == JsonValueKind.String;

        return isString ? value.GetString() : null;
    }
}
