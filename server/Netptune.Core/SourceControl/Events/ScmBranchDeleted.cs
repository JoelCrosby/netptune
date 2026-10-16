using Netptune.Core.SourceControl.Models;

namespace Netptune.Core.SourceControl.Events;

public sealed record ScmBranchDeleted : ScmEvent
{
    public required ScmRepositoryRef Repository { get; init; }

    public required string Branch { get; init; }
}
