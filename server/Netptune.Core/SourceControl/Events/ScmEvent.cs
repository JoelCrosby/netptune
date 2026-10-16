using Netptune.Core.SourceControl.Models;

namespace Netptune.Core.SourceControl.Events;

public abstract record ScmEvent
{
    public required Guid EventId { get; init; }

    public required SourceControlProvider Provider { get; init; }

    public required int ConnectionId { get; init; }

    public required int WorkspaceId { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    public ScmIdentity? Actor { get; init; }
}
