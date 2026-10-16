using Netptune.Core.SourceControl.Models;

namespace Netptune.Core.SourceControl.Events;

public sealed record ScmBranchCreated : ScmEvent
{
    public required ScmRepositoryRef Repository { get; init; }

    public required string Branch { get; init; }

    public required string WebUrl { get; init; }
}
