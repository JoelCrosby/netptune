using Netptune.Core.Entities;

namespace Netptune.Core.ViewModels.Ai;

public sealed class AiChangeRouteIds
{
    public const string Task = "task";
    public const string Project = "project";
    public const string Board = "board";
    public const string Sprint = "sprint";

    public IReadOnlyDictionary<int, string> Tasks { get; init; } = new Dictionary<int, string>();

    public IReadOnlyDictionary<int, string> Projects { get; init; } = new Dictionary<int, string>();

    public IReadOnlyDictionary<int, string> Boards { get; init; } = new Dictionary<int, string>();

    public IReadOnlyDictionary<int, string> Sprints { get; init; } = new Dictionary<int, string>();

    public static List<int> CollectIds(IEnumerable<AiProposedChange> changes, string entityType)
    {
        return changes
            .Where(change => string.Equals(change.EntityType, entityType, StringComparison.Ordinal))
            .Select(change => change.AppliedEntityId ?? change.EntityId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
    }

    public string? Find(string entityType, int? entityId)
    {
        if (!entityId.HasValue)
        {
            return null;
        }

        var lookup = entityType switch
        {
            Task => Tasks,
            Project => Projects,
            Board => Boards,
            Sprint => Sprints,
            _ => null,
        };

        return lookup?.GetValueOrDefault(entityId.Value);
    }
}
