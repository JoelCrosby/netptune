using Netptune.Core.SourceControl.Models;

namespace Netptune.Core.SourceControl.Reading;

public sealed record ScmReadableRepository
{
    public required string FullName { get; init; }

    public SourceControlProvider Provider { get; init; }

    public required string DefaultBranch { get; init; }

    public bool IsPrivate { get; init; }

    public required string WebUrl { get; init; }

    public required IReadOnlyList<string> ProjectKeys { get; init; }
}

public sealed record ScmRepositoryFiles
{
    public required string Repository { get; init; }

    public required string Reference { get; init; }

    public required ScmTree Tree { get; init; }
}

public sealed record ScmRepositoryFile
{
    public required string Repository { get; init; }

    public required string Reference { get; init; }

    public required string CommitSha { get; init; }

    public required ScmFileContent Content { get; init; }
}

public sealed record ScmPullRequestDetail
{
    public required ScmPullRequest PullRequest { get; init; }

    public required IReadOnlyList<ScmPullRequestFile> Files { get; init; }
}
