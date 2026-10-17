namespace Netptune.Core.SourceControl;

public sealed record ScmRepoSyncResult
{
    public int Added { get; init; }

    public int Refreshed { get; init; }

    public int Removed { get; init; }

    public int MarkedInaccessible { get; init; }
}
