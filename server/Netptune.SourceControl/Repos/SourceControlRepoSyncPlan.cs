using Netptune.Core.Entities;

namespace Netptune.SourceControl.Repos;

internal sealed record SourceControlRepoSyncPlan
{
    public required List<SourceControlRepo> Added { get; init; }

    public required List<SourceControlRepo> Removed { get; init; }

    public int Refreshed { get; init; }

    public int MarkedInaccessible { get; init; }
}
