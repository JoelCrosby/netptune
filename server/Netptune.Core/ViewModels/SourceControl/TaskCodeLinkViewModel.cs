using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Models;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Core.ViewModels.SourceControl;

public sealed record TaskCodeLinkViewModel
{
    public long Id { get; init; }

    public TaskCodeLinkKind Kind { get; init; }

    public required string ExternalRef { get; init; }

    public required string Title { get; init; }

    public required string WebUrl { get; init; }

    public string? AuthorLogin { get; init; }

    public AssigneeViewModel? Author { get; init; }

    public ScmPullRequestState? PullRequestState { get; init; }

    public bool IsRemoved { get; init; }

    public DateTime OccurredAt { get; init; }

    public SourceControlProvider Provider { get; init; }

    public required string RepoFullName { get; init; }

    public required string RepoWebUrl { get; init; }

    public ScmCheckSummaryViewModel? Checks { get; init; }
}
