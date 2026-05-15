using Netptune.Core.Events;

namespace Netptune.Core.Models.Search;

public enum SearchIndexOperation { Index, Delete }

public record SearchIndexEvent : IEventMessage
{
    public required SearchIndexOperation Operation { get; init; }

    public required string EntityType { get; init; }

    public required int EntityId { get; init; }

    public required string WorkspaceSlug { get; init; }
}
