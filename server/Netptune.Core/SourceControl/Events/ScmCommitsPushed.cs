using Netptune.Core.SourceControl.Models;

namespace Netptune.Core.SourceControl.Events;

public sealed record ScmCommitsPushed : ScmEvent
{
    public required ScmRepositoryRef Repository { get; init; }

    public required string Branch { get; init; }

    public required IReadOnlyList<ScmCommit> Commits { get; init; }

    public bool Forced { get; init; }
}
