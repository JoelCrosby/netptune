using Netptune.Core.SourceControl.Models;

namespace Netptune.Core.SourceControl.Events;

public sealed record ScmCheckRunChanged : ScmEvent
{
    public required ScmRepositoryRef Repository { get; init; }

    public required ScmCheckRun CheckRun { get; init; }

    public required IReadOnlyList<string> PullRequestNumbers { get; init; }
}
