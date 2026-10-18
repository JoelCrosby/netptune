using System.Text.RegularExpressions;

using Microsoft.Extensions.Caching.Memory;

using Netptune.Core.Entities;
using Netptune.Core.Responses.Common;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Models;
using Netptune.Core.SourceControl.Reading;
using Netptune.Core.UnitOfWork;

namespace Netptune.SourceControl.Reading;

internal sealed partial class SourceControlReader : ISourceControlReader
{
    private const int MaxCachedFileCharacters = 256_000;
    private const int MaxCachedTreeEntries = 20_000;
    private const int MaxPathLength = 1_024;

    private static readonly TimeSpan CommitCacheDuration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan BranchCacheDuration = TimeSpan.FromSeconds(30);

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ISourceControlProviderRegistry Registry;
    private readonly IMemoryCache Cache;

    public SourceControlReader(
        INetptuneUnitOfWork unitOfWork,
        ISourceControlProviderRegistry registry,
        IMemoryCache cache)
    {
        UnitOfWork = unitOfWork;
        Registry = registry;
        Cache = cache;
    }

    public bool HasProviders => Registry.All.Count > 0;

    private sealed record ReadTarget(SourceControlRepo Repo, ScmConnectionContext Context, ISourceControlClient Client)
    {
        public ScmRepositoryRef Ref => new(Repo.ExternalId, Repo.Owner, Repo.Name);
    }

    public async Task<List<ScmReadableRepository>> ListRepositories(int workspaceId, CancellationToken cancellationToken)
    {
        var repos = await UnitOfWork.SourceControlRepos.GetReadableForWorkspace(workspaceId, cancellationToken);
        var projectUrls = await UnitOfWork.Projects.GetRepositoryUrls(workspaceId, cancellationToken);

        var readable = repos
            .Where(repo => Registry.IsEnabled(repo.Connection.Provider))
            .Select(repo => new ScmReadableRepository
            {
                FullName = repo.FullName,
                Provider = repo.Connection.Provider,
                DefaultBranch = repo.DefaultBranch,
                IsPrivate = repo.IsPrivate,
                WebUrl = repo.WebUrl,
                ProjectKeys = projectUrls
                    .Where(project => IsSameRepository(project.RepositoryUrl, repo.WebUrl))
                    .Select(project => project.ProjectKey)
                    .ToList(),
            })
            .ToList();

        return readable;
    }

    public Task<ClientResponse<ScmRepositoryFiles>> ListFiles(
        int workspaceId,
        string repository,
        string? reference,
        CancellationToken cancellationToken)
    {
        return Read(workspaceId, repository, SourceControlCapabilities.ReadCommits, async target =>
        {
            var resolved = await ResolveCommit(target, reference, cancellationToken);

            if (!resolved.IsSuccess)
            {
                return ClientResponse<ScmRepositoryFiles>.Failed(resolved.Message);
            }

            var commitSha = resolved.Payload!;
            var tree = await GetTree(target, commitSha, cancellationToken);

            var files = new ScmRepositoryFiles
            {
                Repository = target.Repo.FullName,
                Reference = reference ?? target.Repo.DefaultBranch,
                Tree = tree,
            };

            return ClientResponse<ScmRepositoryFiles>.Success(files);
        }, cancellationToken);
    }

    public Task<ClientResponse<ScmRepositoryFile>> ReadFile(
        int workspaceId,
        string repository,
        string path,
        string? reference,
        CancellationToken cancellationToken)
    {
        var isSafePath = IsSafePath(path.Trim().TrimStart('/'));

        if (!isSafePath)
        {
            return Task.FromResult(ClientResponse<ScmRepositoryFile>.Failed(UnsafePathMessage(path)));
        }

        return Read(workspaceId, repository, SourceControlCapabilities.ReadCommits, async target =>
        {
            var resolved = await ResolveCommit(target, reference, cancellationToken);

            if (!resolved.IsSuccess)
            {
                return ClientResponse<ScmRepositoryFile>.Failed(resolved.Message);
            }

            var commitSha = resolved.Payload!;
            var normalizedPath = path.Trim().TrimStart('/');
            var content = await GetFile(target, normalizedPath, commitSha, cancellationToken);

            if (content is null)
            {
                return ClientResponse<ScmRepositoryFile>.Failed(
                    $"{normalizedPath} is not a file in {target.Repo.FullName} at {reference ?? target.Repo.DefaultBranch}.");
            }

            var file = new ScmRepositoryFile
            {
                Repository = target.Repo.FullName,
                Reference = reference ?? target.Repo.DefaultBranch,
                CommitSha = commitSha,
                Content = content,
            };

            return ClientResponse<ScmRepositoryFile>.Success(file);
        }, cancellationToken);
    }

    public Task<ClientResponse<IReadOnlyList<ScmCodeMatch>>> SearchCode(
        int workspaceId,
        string repository,
        string query,
        CancellationToken cancellationToken)
    {
        var namesAnotherScope = ScopeQualifierPattern().IsMatch(query);

        if (namesAnotherScope)
        {
            return Task.FromResult(ClientResponse<IReadOnlyList<ScmCodeMatch>>.Failed(
                $"A search can't name a repository, organisation or user; it is already limited to {repository}. "
                + "Remove the qualifier and search again."));
        }

        return Read(workspaceId, repository, SourceControlCapabilities.ReadCommits, async target =>
        {
            var matches = await target.Client.SearchCode(target.Context, target.Ref, query, cancellationToken);

            return ClientResponse<IReadOnlyList<ScmCodeMatch>>.Success(matches);
        }, cancellationToken);
    }

    public Task<ClientResponse<IReadOnlyList<ScmCommit>>> ListCommits(
        int workspaceId,
        string repository,
        string? branch,
        int take,
        CancellationToken cancellationToken)
    {
        return Read(workspaceId, repository, SourceControlCapabilities.ReadCommits, async target =>
        {
            var query = new ScmCommitQuery(branch ?? target.Repo.DefaultBranch, null, take);
            var commits = await target.Client.ListCommits(target.Context, target.Ref, query, cancellationToken);

            return ClientResponse<IReadOnlyList<ScmCommit>>.Success(commits);
        }, cancellationToken);
    }

    public Task<ClientResponse<ScmPullRequestDetail>> GetPullRequest(
        int workspaceId,
        string repository,
        string number,
        CancellationToken cancellationToken)
    {
        return Read(workspaceId, repository, SourceControlCapabilities.ReadPullRequests, async target =>
        {
            var pullRequest = await target.Client.GetPullRequest(target.Context, target.Ref, number, cancellationToken);

            if (pullRequest is null)
            {
                return ClientResponse<ScmPullRequestDetail>.Failed(
                    $"Pull request {number} was not found in {target.Repo.FullName}.");
            }

            var files = await target.Client.ListPullRequestFiles(target.Context, target.Ref, number, cancellationToken);
            var detail = new ScmPullRequestDetail { PullRequest = pullRequest, Files = files };

            return ClientResponse<ScmPullRequestDetail>.Success(detail);
        }, cancellationToken);
    }

    private async Task<ClientResponse<T>> Read<T>(
        int workspaceId,
        string repository,
        SourceControlCapabilities capability,
        Func<ReadTarget, Task<ClientResponse<T>>> read,
        CancellationToken cancellationToken)
    {
        var repos = await UnitOfWork.SourceControlRepos.GetReadableForWorkspace(workspaceId, cancellationToken);
        var repo = repos.FirstOrDefault(candidate => IsSameName(candidate.FullName, repository));

        if (repo is null)
        {
            return ClientResponse<T>.Failed(
                $"No linked repository is named {repository}. Call list_repositories for the ones that can be read.");
        }

        var isEnabled = Registry.IsEnabled(repo.Connection.Provider);

        if (!isEnabled)
        {
            return ClientResponse<T>.Failed($"{repo.Connection.Provider} is not enabled on this Netptune instance.");
        }

        var connection = repo.Connection.ToScmConnection();
        var requirement = Registry.Require(connection, capability);

        if (!requirement.IsSuccess)
        {
            return ClientResponse<T>.Failed(requirement.Message);
        }

        var provider = Registry.Resolve(repo.Connection.Provider);
        var target = new ReadTarget(repo, Registry.CreateContext(connection), provider.Client);

        try
        {
            var result = await read(target);

            return result;
        }
        catch (ScmRateLimitedException exception)
        {
            return ClientResponse<T>.Failed(
                $"{repo.Connection.Provider}'s rate limit is used up until {exception.RetryAt:u}. Try again later.");
        }
        catch (ScmProviderException exception)
        {
            return ClientResponse<T>.Failed($"{repo.Connection.Provider} could not complete the request: {exception.Message}");
        }
    }

    private async Task<ClientResponse<string>> ResolveCommit(
        ReadTarget target,
        string? reference,
        CancellationToken cancellationToken)
    {
        var branch = string.IsNullOrWhiteSpace(reference) ? target.Repo.DefaultBranch : reference.Trim();
        var isCommitSha = CommitShaPattern().IsMatch(branch);

        if (isCommitSha)
        {
            return ClientResponse<string>.Success(payload: branch);
        }

        var isSafeBranch = IsSafePath(branch);

        if (!isSafeBranch)
        {
            return ClientResponse<string>.Failed($"{branch} is not a valid branch name.");
        }

        var head = await Cache.GetOrCreateAsync(
            $"scm:branch:{target.Repo.Id}:{branch}",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = BranchCacheDuration;

                var found = await target.Client.GetBranch(target.Context, target.Ref, branch, cancellationToken);

                return found?.HeadSha;
            });

        if (head is null)
        {
            return ClientResponse<string>.Failed($"Branch {branch} was not found in {target.Repo.FullName}.");
        }

        return ClientResponse<string>.Success(payload: head);
    }

    private async Task<ScmTree> GetTree(ReadTarget target, string commitSha, CancellationToken cancellationToken)
    {
        var key = $"scm:tree:{target.Repo.Id}:{commitSha}";

        if (Cache.TryGetValue(key, out ScmTree? cached))
        {
            return cached!;
        }

        var tree = await target.Client.GetTree(target.Context, target.Ref, commitSha, cancellationToken);
        var isSmallEnoughToCache = tree.Entries.Count <= MaxCachedTreeEntries;

        if (isSmallEnoughToCache)
        {
            Cache.Set(key, tree, CommitCacheDuration);
        }

        return tree;
    }

    // Large files are left uncached, because the cache is shared with the rest of the App.
    private async Task<ScmFileContent?> GetFile(
        ReadTarget target,
        string path,
        string commitSha,
        CancellationToken cancellationToken)
    {
        var key = $"scm:file:{target.Repo.Id}:{commitSha}:{path}";

        if (Cache.TryGetValue(key, out ScmFileContent? cached))
        {
            return cached;
        }

        var content = await target.Client.GetFile(target.Context, target.Ref, path, commitSha, cancellationToken);
        var isSmallEnoughToCache = (content?.Text?.Length ?? 0) <= MaxCachedFileCharacters;

        if (isSmallEnoughToCache)
        {
            Cache.Set(key, content, CommitCacheDuration);
        }

        return content;
    }

    // The provider is called with the installation's token, which reaches every repository in the
    // installation, so a path or branch must not be able to step outside the linked one with "..".
    private static bool IsSafePath(string path)
    {
        var hasValidLength = path.Length is > 0 and <= MaxPathLength;
        var hasUnsafeCharacters = path.Any(character => character == '\\' || char.IsControl(character));
        var segments = path.Split('/');
        var hasUnsafeSegment = segments.Any(segment => segment is "" or "." or "..");

        return hasValidLength && !hasUnsafeCharacters && !hasUnsafeSegment;
    }

    private static string UnsafePathMessage(string path)
    {
        return $"{path} is not a valid path. Give it from the repository root, without \".\" or \"..\" segments.";
    }

    private static bool IsSameName(string fullName, string requested)
    {
        return string.Equals(fullName, requested.Trim().Trim('/'), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSameRepository(string projectUrl, string repoWebUrl)
    {
        var normalizedProject = NormalizeUrl(projectUrl);
        var normalizedRepo = NormalizeUrl(repoWebUrl);

        return string.Equals(normalizedProject, normalizedRepo, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeUrl(string url)
    {
        var trimmed = url.Trim().TrimEnd('/');
        var withoutSuffix = trimmed.EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? trimmed[..^4] : trimmed;
        var sshForm = SshUrlPattern().Match(withoutSuffix);

        return sshForm.Success
            ? $"https://{sshForm.Groups["host"].Value}/{sshForm.Groups["path"].Value}"
            : withoutSuffix;
    }

    [GeneratedRegex("^[0-9a-fA-F]{40}$")]
    private static partial Regex CommitShaPattern();

    [GeneratedRegex(@"(^|[\s(])-?(repo|org|user|owner|enterprise):", RegexOptions.IgnoreCase)]
    private static partial Regex ScopeQualifierPattern();

    [GeneratedRegex("^git@(?<host>[^:]+):(?<path>.+)$")]
    private static partial Regex SshUrlPattern();
}
