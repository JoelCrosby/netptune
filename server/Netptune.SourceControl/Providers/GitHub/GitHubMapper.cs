using System.Globalization;

using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Models;
using Netptune.SourceControl.Providers.GitHub.Dtos;

namespace Netptune.SourceControl.Providers.GitHub;

internal static class GitHubMapper
{
    public static ScmRepositoryRef ToRepositoryRef(GitHubRepositoryDto repository)
    {
        var externalId = ToExternalId(repository.Id);

        return new ScmRepositoryRef(externalId, repository.Owner.Login, repository.Name);
    }

    public static ScmRepositoryRef ToRepositoryRef(GitHubRepositoryRefDto repository)
    {
        var externalId = ToExternalId(repository.Id);
        var separatorIndex = repository.FullName.IndexOf('/');
        var owner = separatorIndex > 0 ? repository.FullName[..separatorIndex] : string.Empty;

        return new ScmRepositoryRef(externalId, owner, repository.Name);
    }

    public static ScmRepository ToRepository(GitHubRepositoryDto repository)
    {
        return new ScmRepository
        {
            Ref = ToRepositoryRef(repository),
            FullName = repository.FullName,
            DefaultBranch = repository.DefaultBranch ?? string.Empty,
            WebUrl = repository.HtmlUrl,
            IsPrivate = repository.Private,
            IsArchived = repository.Archived,
        };
    }

    public static ScmIdentity ToIdentity(GitHubAccountDto account)
    {
        return new ScmIdentity
        {
            Provider = SourceControlProvider.GitHub,
            ExternalId = ToExternalId(account.Id),
            Username = account.Login,
            AvatarUrl = account.AvatarUrl,
        };
    }

    public static ScmIdentity ToIdentity(GitHubPushAuthorDto author)
    {
        return new ScmIdentity
        {
            Provider = SourceControlProvider.GitHub,
            Username = author.Username ?? author.Name ?? string.Empty,
            DisplayName = author.Name,
            Email = author.Email,
        };
    }

    public static ScmCommit ToCommit(GitHubCommitDto commit)
    {
        var gitAuthor = commit.Commit.Author;
        var accountIdentity = commit.Author is null ? null : ToIdentity(commit.Author);

        var gitIdentity = new ScmIdentity
        {
            Provider = SourceControlProvider.GitHub,
            Username = gitAuthor?.Name ?? string.Empty,
            DisplayName = gitAuthor?.Name,
            Email = gitAuthor?.Email,
        };

        var author = accountIdentity is null
            ? gitIdentity
            : accountIdentity with { DisplayName = gitAuthor?.Name, Email = gitAuthor?.Email };

        return new ScmCommit
        {
            Sha = commit.Sha,
            Message = commit.Commit.Message,
            Author = author,
            AuthoredAt = gitAuthor?.Date ?? DateTimeOffset.MinValue,
            WebUrl = commit.HtmlUrl,
        };
    }

    public static ScmCommit ToCommit(GitHubPushCommitDto commit)
    {
        return new ScmCommit
        {
            Sha = commit.Id,
            Message = commit.Message,
            Author = ToIdentity(commit.Author),
            AuthoredAt = commit.Timestamp,
            WebUrl = commit.Url,
        };
    }

    public static ScmPullRequest ToPullRequest(GitHubPullRequestDto pullRequest)
    {
        var author = pullRequest.User is null
            ? new ScmIdentity { Provider = SourceControlProvider.GitHub, Username = string.Empty }
            : ToIdentity(pullRequest.User);

        return new ScmPullRequest
        {
            ExternalId = ToExternalId(pullRequest.Id),
            Number = pullRequest.Number.ToString(CultureInfo.InvariantCulture),
            Title = pullRequest.Title,
            Body = pullRequest.Body,
            State = ToPullRequestState(pullRequest),
            SourceBranch = pullRequest.Head.Ref,
            HeadSha = pullRequest.Head.Sha,
            TargetBranch = pullRequest.Base.Ref,
            Author = author,
            WebUrl = pullRequest.HtmlUrl,
            CreatedAt = pullRequest.CreatedAt,
            UpdatedAt = pullRequest.UpdatedAt,
            MergedAt = pullRequest.MergedAt,
            ClosedAt = pullRequest.ClosedAt,
        };
    }

    public static ScmCheckRun ToCheckRun(GitHubCheckRunDto checkRun)
    {
        return new ScmCheckRun
        {
            ExternalId = ToExternalId(checkRun.Id),
            Name = checkRun.Name,
            Status = ToCheckRunStatus(checkRun.Status),
            Conclusion = ToCheckRunConclusion(checkRun.Conclusion),
            CommitSha = checkRun.HeadSha,
            WebUrl = checkRun.HtmlUrl,
            CompletedAt = checkRun.CompletedAt,
        };
    }

    public static ScmBranch ToBranch(GitHubBranchDto branch, string webBaseUrl, ScmRepositoryRef repository)
    {
        var webUrl = GitHubUrls.Branch(webBaseUrl, repository, branch.Name);

        return new ScmBranch(branch.Name, branch.Commit.Sha, webUrl);
    }

    public static string ToExternalId(long id)
    {
        return id.ToString(CultureInfo.InvariantCulture);
    }

    private static ScmPullRequestState ToPullRequestState(GitHubPullRequestDto pullRequest)
    {
        var isMerged = pullRequest.Merged == true || pullRequest.MergedAt is not null;
        var isClosed = pullRequest.State == "closed";

        if (isMerged)
        {
            return ScmPullRequestState.Merged;
        }

        if (isClosed)
        {
            return ScmPullRequestState.Closed;
        }

        return pullRequest.Draft ? ScmPullRequestState.Draft : ScmPullRequestState.Open;
    }

    private static ScmCheckRunStatus ToCheckRunStatus(string status)
    {
        return status switch
        {
            "completed" => ScmCheckRunStatus.Completed,
            "in_progress" => ScmCheckRunStatus.InProgress,
            _ => ScmCheckRunStatus.Queued,
        };
    }

    private static ScmCheckRunConclusion? ToCheckRunConclusion(string? conclusion)
    {
        return conclusion switch
        {
            "success" => ScmCheckRunConclusion.Success,
            "failure" or "action_required" or "startup_failure" => ScmCheckRunConclusion.Failure,
            "cancelled" => ScmCheckRunConclusion.Cancelled,
            "skipped" => ScmCheckRunConclusion.Skipped,
            "neutral" or "stale" => ScmCheckRunConclusion.Neutral,
            "timed_out" => ScmCheckRunConclusion.TimedOut,
            _ => null,
        };
    }
}
