using Netptune.Core.SourceControl;

namespace Netptune.Core.ViewModels.SourceControl;

public sealed record SourceControlRepoViewModel
{
    public int Id { get; init; }

    public int ConnectionId { get; init; }

    public required string Owner { get; init; }

    public required string Name { get; init; }

    public required string FullName { get; init; }

    public required string DefaultBranch { get; init; }

    public required string WebUrl { get; init; }

    public bool IsPrivate { get; init; }

    public bool IsArchived { get; init; }

    public bool IsAccessible { get; init; }

    public bool IsLinked { get; init; }

    public DateTime? LinkedAt { get; init; }

    public DateTime SyncedAt { get; init; }

    public SourceControlHistorySyncStatus HistorySyncStatus { get; init; }

    public DateTime? HistorySyncedAt { get; init; }
}
