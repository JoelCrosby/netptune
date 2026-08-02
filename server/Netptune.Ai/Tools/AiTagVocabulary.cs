using System.Text.Json;

using Mediator;

using Netptune.Core.Services.Ai;
using Netptune.Handlers.Tags.Queries;

namespace Netptune.Ai.Tools;

public static class AiTagVocabulary
{
    public static async Task<HashSet<string>?> Read(
        IMediator mediator,
        IAiChangeSetBuilder changeSet,
        CancellationToken cancellationToken)
    {
        var workspaceTags = await mediator.Send(new GetTagsForWorkspaceQuery(), cancellationToken);

        if (workspaceTags is null)
        {
            return null;
        }

        var names = workspaceTags.Select(tag => tag.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var proposed in CreateTagTool.ProposedNames(changeSet))
        {
            names.Add(proposed);
        }

        return names;
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

    public static string? FindUnknown(IReadOnlyCollection<string> requested, HashSet<string> known)
    {
        var unknown = requested.Where(tag => !known.Contains(tag)).ToList();

        if (unknown.Count == 0)
        {
            return null;
        }

        return $"These tags do not exist in this workspace: {string.Join(", ", unknown)}. "
            + "Use propose_create_tag first if the workspace needs a new one.";
    }
}
