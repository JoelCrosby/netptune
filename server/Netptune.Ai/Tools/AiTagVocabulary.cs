using System.Text.Json;

using Mediator;

using Netptune.Core.Services.Ai;
using Netptune.Handlers.Tags.Queries;

namespace Netptune.Ai.Tools;

public static class AiTagVocabulary
{
    public static async Task<string?> FindUnknown(
        IMediator mediator,
        IAiChangeSetBuilder changeSet,
        IReadOnlyCollection<string> requested,
        CancellationToken cancellationToken)
    {
        var proposedNames = CreateTagAction.ProposedNames(changeSet).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var notProposed = requested.Where(name => !proposedNames.Contains(name)).ToList();

        if (notProposed.Count == 0)
        {
            return null;
        }

        var existing = await mediator.Send(new GetTagsByNameQuery(notProposed), cancellationToken);
        var existingNames = existing.Select(tag => tag.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknown = notProposed.Where(name => !existingNames.Contains(name)).ToList();

        if (unknown.Count == 0)
        {
            return null;
        }

        return $"These tags do not exist in this workspace: {string.Join(", ", unknown)}. "
            + "Use propose_tag_change with action create first if the workspace needs a new one.";
    }

    public static List<string> ReadRequested(JsonElement arguments)
    {
        var isObject = arguments.ValueKind == JsonValueKind.Object;

        if (!isObject)
        {
            return [];
        }

        var hasProperty = arguments.TryGetProperty("tags", out var value) && value.ValueKind == JsonValueKind.Array;

        if (!hasProperty)
        {
            return [];
        }

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString()!)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList();
    }
}
