using Netptune.Core.Enums;

namespace Netptune.Core.Models.Activity;

public sealed record ActivityEntryUpsert
{
    public required int WorkspaceId { get; init; }

    public required EntityType EntityType { get; init; }

    public required int EntityId { get; init; }

    public required string UserId { get; init; }

    public required ActivityType ActivityType { get; init; }

    public required List<string> ChangedFields { get; init; }

    public required string MetaJson { get; init; }

    public required long LastEventRecordId { get; init; }

    public required DateTime FirstOccurredAt { get; init; }

    public required DateTime LastOccurredAt { get; init; }

    public required int RevisionCount { get; init; }

    public string? WorkspaceSlug { get; init; }

    public int? ProjectId { get; init; }

    public string? ProjectSlug { get; init; }

    public int? BoardId { get; init; }

    public string? BoardSlug { get; init; }

    public int? BoardGroupId { get; init; }

    public int? TaskId { get; init; }
}
