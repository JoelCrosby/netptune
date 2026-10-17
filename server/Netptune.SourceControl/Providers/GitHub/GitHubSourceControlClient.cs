using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;

using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Models;
using Netptune.SourceControl.Providers.GitHub.Dtos;

namespace Netptune.SourceControl.Providers.GitHub;

internal sealed class GitHubSourceControlClient : ISourceControlClient
{
    private const int PageSize = 100;
    private const int MaxBranches = 1000;
    private const int MaxCheckRuns = 100;

    private readonly GitHubApi Api;

    public GitHubSourceControlClient(GitHubApi api)
    {
        Api = api;
    }

    public async IAsyncEnumerable<ScmRepository> ListRepositories(
        ScmConnectionContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string? url = $"/installation/repositories?per_page={PageSize}";

        while (url is not null)
        {
            var response = await Get<GitHubInstallationRepositoriesDto>(context, url, cancellationToken);
            var repositories = response.Payload?.Repositories ?? [];

            foreach (var repository in repositories)
            {
                yield return GitHubMapper.ToRepository(repository);
            }

            url = response.NextPageUrl;
        }
    }

    public async ValueTask<IReadOnlyList<ScmBranch>> ListBranches(
        ScmConnectionContext context,
        ScmRepositoryRef repository,
        CancellationToken cancellationToken)
    {
        var url = $"{GitHubUrls.Repository(repository)}/branches?per_page={PageSize}";
        var branches = await CollectPages<GitHubBranchDto>(context, url, MaxBranches, cancellationToken);

        return branches
            .Select(branch => GitHubMapper.ToBranch(branch, Api.WebBaseUrl, repository))
            .ToList();
    }

    public async ValueTask<ScmBranch?> GetBranch(
        ScmConnectionContext context,
        ScmRepositoryRef repository,
        string name,
        CancellationToken cancellationToken)
    {
        var url = $"{GitHubUrls.Repository(repository)}/branches/{GitHubUrls.EscapePath(name)}";
        var response = await Get<GitHubBranchDto>(context, url, cancellationToken);

        if (response.Payload is null)
        {
            return null;
        }

        return GitHubMapper.ToBranch(response.Payload, Api.WebBaseUrl, repository);
    }

    public async ValueTask<IReadOnlyList<ScmCommit>> ListCommits(
        ScmConnectionContext context,
        ScmRepositoryRef repository,
        ScmCommitQuery query,
        CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(query.Take, 1, PageSize);
        var parameters = new List<string> { $"per_page={pageSize}" };

        if (query.Branch is not null)
        {
            parameters.Add($"sha={Uri.EscapeDataString(query.Branch)}");
        }

        if (query.Since is not null)
        {
            var since = query.Since.Value.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);

            parameters.Add($"since={Uri.EscapeDataString(since)}");
        }

        var url = $"{GitHubUrls.Repository(repository)}/commits?{string.Join('&', parameters)}";
        var commits = await CollectPages<GitHubCommitDto>(context, url, query.Take, cancellationToken);

        return commits.Select(GitHubMapper.ToCommit).ToList();
    }

    public async ValueTask<ScmPullRequest?> GetPullRequest(
        ScmConnectionContext context,
        ScmRepositoryRef repository,
        string number,
        CancellationToken cancellationToken)
    {
        var url = $"{GitHubUrls.Repository(repository)}/pulls/{Uri.EscapeDataString(number)}";
        var response = await Get<GitHubPullRequestDto>(context, url, cancellationToken);

        if (response.Payload is null)
        {
            return null;
        }

        return GitHubMapper.ToPullRequest(response.Payload);
    }

    public async ValueTask<IReadOnlyList<ScmPullRequest>> ListPullRequests(
        ScmConnectionContext context,
        ScmRepositoryRef repository,
        ScmPullRequestQuery query,
        CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(query.Take, 1, PageSize);
        var state = query.IncludeClosed ? "all" : "open";
        var parameters = new List<string> { $"state={state}", $"per_page={pageSize}" };

        if (query.SourceBranch is not null)
        {
            var head = $"{repository.Owner}:{query.SourceBranch}";

            parameters.Add($"head={Uri.EscapeDataString(head)}");
        }

        var url = $"{GitHubUrls.Repository(repository)}/pulls?{string.Join('&', parameters)}";
        var pullRequests = await CollectPages<GitHubPullRequestDto>(context, url, query.Take, cancellationToken);

        return pullRequests.Select(GitHubMapper.ToPullRequest).ToList();
    }

    public async ValueTask<IReadOnlyList<ScmCheckRun>> ListCheckRuns(
        ScmConnectionContext context,
        ScmRepositoryRef repository,
        string commitSha,
        CancellationToken cancellationToken)
    {
        var commitPath = $"{GitHubUrls.Repository(repository)}/commits/{Uri.EscapeDataString(commitSha)}";
        var url = $"{commitPath}/check-runs?per_page={MaxCheckRuns}";
        var response = await Get<GitHubCheckRunsDto>(context, url, cancellationToken);
        var checkRuns = response.Payload?.CheckRuns ?? [];

        return checkRuns.Select(GitHubMapper.ToCheckRun).ToList();
    }

    private async Task<GitHubResponse<T>> Get<T>(
        ScmConnectionContext context,
        string url,
        CancellationToken cancellationToken)
    {
        var token = await context.GetAccessToken(cancellationToken);

        var request = new GitHubRequest
        {
            Method = HttpMethod.Get,
            Url = url,
            BearerToken = token.Token,
        };

        var response = await Api.Send<T>(request, cancellationToken);
        var isUnprocessable = response.StatusCode == HttpStatusCode.UnprocessableEntity;

        if (isUnprocessable)
        {
            throw new ScmProviderException(
                SourceControlProvider.GitHub,
                (int)response.StatusCode,
                response.ErrorMessage);
        }

        return response;
    }

    private async Task<List<T>> CollectPages<T>(
        ScmConnectionContext context,
        string firstUrl,
        int take,
        CancellationToken cancellationToken)
    {
        var items = new List<T>();
        string? url = firstUrl;

        while (url is not null && items.Count < take)
        {
            var response = await Get<List<T>>(context, url, cancellationToken);
            var page = response.Payload ?? [];

            items.AddRange(page);
            url = response.NextPageUrl;
        }

        return items.Take(take).ToList();
    }
}
