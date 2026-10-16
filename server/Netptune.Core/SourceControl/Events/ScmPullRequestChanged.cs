using Netptune.Core.SourceControl.Models;

namespace Netptune.Core.SourceControl.Events;

public sealed record ScmPullRequestChanged : ScmEvent
{
    public required ScmRepositoryRef Repository { get; init; }

    public required ScmPullRequest PullRequest { get; init; }

    public ScmPullRequestAction Action { get; init; }
}
