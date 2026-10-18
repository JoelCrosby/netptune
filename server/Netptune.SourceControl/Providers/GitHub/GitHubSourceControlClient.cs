using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Models;
using Netptune.SourceControl.Providers.GitHub.Dtos;

namespace Netptune.SourceControl.Providers.GitHub;

internal sealed class GitHubSourceControlClient : ISourceControlClient
{
    private const int PageSize = 100;
    private const int MaxComparePages = 3;
    private const int MaxBranches = 1000;
    private const int MaxCheckRuns = 100;
    private const int MaxSearchResults = 20;
    private const int MaxPullRequestFiles = 300;
    private const int BinarySniffLength = 8000;
    private const string TextMatchMediaType = "application/vnd.github.text-match+json";

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

    // Only the commits on the head branch that its base doesn't have, oldest first, as a push would report them.
    // Pages run oldest first too, so every page is read to reach the newest; GitHub stops at 250 commits.
    public async ValueTask<IReadOnlyList<ScmCommit>> CompareCommits(
        ScmConnectionContext context,
        ScmRepositoryRef repository,
        string baseBranch,
        string headBranch,
        CancellationToken cancellationToken)
    {
        var range = $"{GitHubUrls.EscapePath(baseBranch)}...{GitHubUrls.EscapePath(headBranch)}";
        var commits = new List<GitHubCommitDto>();
        string? url = $"{GitHubUrls.Repository(repository)}/compare/{range}?per_page={PageSize}";

        for (var page = 0; url is not null && page < MaxComparePages; page++)
        {
            var response = await Get<GitHubCompareDto>(context, url, cancellationToken);

            commits.AddRange(response.Payload?.Commits ?? []);
            url = response.NextPageUrl;
        }

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

        if (query.UpdatedSince is not null)
        {
            parameters.Add("sort=updated");
            parameters.Add("direction=desc");
        }

        if (query.SourceBranch is not null)
        {
            var head = $"{repository.Owner}:{query.SourceBranch}";

            parameters.Add($"head={Uri.EscapeDataString(head)}");
        }

        var url = $"{GitHubUrls.Repository(repository)}/pulls?{string.Join('&', parameters)}";
        var pullRequests = await CollectPages<GitHubPullRequestDto>(context, url, query.Take, cancellationToken);
        var mapped = pullRequests.Select(GitHubMapper.ToPullRequest);

        return query.UpdatedSince is null
            ? mapped.ToList()
            : mapped.Where(pullRequest => pullRequest.UpdatedAt >= query.UpdatedSince).ToList();
    }

    public async ValueTask<ScmTree> GetTree(
        ScmConnectionContext context,
        ScmRepositoryRef repository,
        string commitSha,
        CancellationToken cancellationToken)
    {
        var url = $"{GitHubUrls.Repository(repository)}/git/trees/{Uri.EscapeDataString(commitSha)}?recursive=1";
        var response = await Get<GitHubTreeDto>(context, url, cancellationToken);
        var tree = response.Payload ?? new GitHubTreeDto { Sha = commitSha };

        var entries = tree.Tree
            .Where(entry => entry.Type is "blob" or "tree")
            .Select(entry => new ScmTreeEntry(entry.Path, entry.Type == "tree", entry.Size))
            .ToList();

        return new ScmTree
        {
            CommitSha = commitSha,
            Entries = entries,
            IsTruncated = tree.Truncated,
        };
    }

    public async ValueTask<ScmFileContent?> GetFile(
        ScmConnectionContext context,
        ScmRepositoryRef repository,
        string path,
        string commitSha,
        CancellationToken cancellationToken)
    {
        var contentsPath = $"{GitHubUrls.Repository(repository)}/contents/{GitHubUrls.EscapePath(path)}";
        var url = $"{contentsPath}?ref={Uri.EscapeDataString(commitSha)}";
        var content = await GetContent(context, url, cancellationToken);
        var isFile = content is { Type: "file" };

        if (!isFile)
        {
            return null;
        }

        return ToFileContent(content!);
    }

    public async ValueTask<IReadOnlyList<ScmCodeMatch>> SearchCode(
        ScmConnectionContext context,
        ScmRepositoryRef repository,
        string query,
        CancellationToken cancellationToken)
    {
        var scopedQuery = $"{query} repo:{repository.Owner}/{repository.Name}";
        var url = $"/search/code?q={Uri.EscapeDataString(scopedQuery)}&per_page={MaxSearchResults}";
        var response = await Get<GitHubCodeSearchDto>(context, url, cancellationToken, TextMatchMediaType);
        var items = response.Payload?.Items ?? [];

        return items.Select(ToCodeMatch).ToList();
    }

    public async ValueTask<IReadOnlyList<ScmPullRequestFile>> ListPullRequestFiles(
        ScmConnectionContext context,
        ScmRepositoryRef repository,
        string number,
        CancellationToken cancellationToken)
    {
        var url = $"{GitHubUrls.Repository(repository)}/pulls/{Uri.EscapeDataString(number)}/files?per_page={PageSize}";
        var files = await CollectPages<GitHubPullRequestFileDto>(context, url, MaxPullRequestFiles, cancellationToken);

        return files.Select(ToPullRequestFile).ToList();
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

    // A directory's contents come back as an array rather than one entry, which is not a file either.
    private async Task<GitHubContentDto?> GetContent(
        ScmConnectionContext context,
        string url,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await Get<GitHubContentDto>(context, url, cancellationToken);

            return response.Payload;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static ScmFileContent ToFileContent(GitHubContentDto content)
    {
        var hasInlineContent = content.Encoding == "base64" && content.Content is not null;

        if (!hasInlineContent)
        {
            return new ScmFileContent
            {
                Path = content.Path,
                Size = content.Size,
                IsTooLarge = true,
                WebUrl = content.HtmlUrl,
            };
        }

        var bytes = Convert.FromBase64String(content.Content!);
        var sniffLength = Math.Min(bytes.Length, BinarySniffLength);
        var isBinary = Array.IndexOf(bytes, (byte)0, 0, sniffLength) >= 0;

        return new ScmFileContent
        {
            Path = content.Path,
            Size = content.Size,
            Text = isBinary ? null : Encoding.UTF8.GetString(bytes),
            IsBinary = isBinary,
            WebUrl = content.HtmlUrl,
        };
    }

    private static ScmCodeMatch ToCodeMatch(GitHubCodeSearchItemDto item)
    {
        var fragments = (item.TextMatches ?? [])
            .Select(match => match.Fragment)
            .OfType<string>()
            .ToList();

        return new ScmCodeMatch(item.Path, fragments, item.HtmlUrl);
    }

    private static ScmPullRequestFile ToPullRequestFile(GitHubPullRequestFileDto file)
    {
        return new ScmPullRequestFile
        {
            Path = file.Filename,
            Status = file.Status,
            Additions = file.Additions,
            Deletions = file.Deletions,
            Patch = file.Patch,
        };
    }

    private async Task<GitHubResponse<T>> Get<T>(
        ScmConnectionContext context,
        string url,
        CancellationToken cancellationToken,
        string? accept = null)
    {
        var token = await context.GetAccessToken(cancellationToken);

        var request = new GitHubRequest
        {
            Method = HttpMethod.Get,
            Url = url,
            BearerToken = token.Token,
            Accept = accept ?? GitHubRequest.DefaultAccept,
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
