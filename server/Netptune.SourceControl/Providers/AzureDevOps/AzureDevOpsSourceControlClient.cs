using System.Net;
using System.Runtime.CompilerServices;

using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Models;
using Netptune.SourceControl.Providers.AzureDevOps.Dtos;

namespace Netptune.SourceControl.Providers.AzureDevOps;

// Capability gating means only ListRepositories is ever called until the other reads are added.
internal sealed class AzureDevOpsSourceControlClient : ISourceControlClient
{
    private const string RepositoriesPath = "_apis/git/repositories?api-version=7.1";

    private readonly AzureDevOpsApi Api;

    public AzureDevOpsSourceControlClient(AzureDevOpsApi api)
    {
        Api = api;
    }

    public async IAsyncEnumerable<ScmRepository> ListRepositories(
        ScmConnectionContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var credential = await CreateCredential(context, cancellationToken);
        var request = credential.CreateRequest(RepositoriesPath);
        var response = await Api.Get<AzureDevOpsListDto<AzureDevOpsRepositoryDto>>(request, cancellationToken);

        ThrowIfFailed(response);

        var repositories = response.Payload!.Value.Where(repository => !repository.IsDisabled);

        foreach (var repository in repositories)
        {
            yield return AzureDevOpsMapper.ToRepository(repository);
        }
    }

    public ValueTask<IReadOnlyList<ScmBranch>> ListBranches(
        ScmConnectionContext context,
        ScmRepositoryRef repository,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public ValueTask<ScmBranch?> GetBranch(
        ScmConnectionContext context,
        ScmRepositoryRef repository,
        string name,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public ValueTask<IReadOnlyList<ScmCommit>> ListCommits(
        ScmConnectionContext context,
        ScmRepositoryRef repository,
        ScmCommitQuery query,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public ValueTask<IReadOnlyList<ScmCommit>> CompareCommits(
        ScmConnectionContext context,
        ScmRepositoryRef repository,
        string baseBranch,
        string headBranch,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public ValueTask<ScmPullRequest?> GetPullRequest(
        ScmConnectionContext context,
        ScmRepositoryRef repository,
        string number,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public ValueTask<IReadOnlyList<ScmPullRequest>> ListPullRequests(
        ScmConnectionContext context,
        ScmRepositoryRef repository,
        ScmPullRequestQuery query,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public ValueTask<ScmTree> GetTree(
        ScmConnectionContext context,
        ScmRepositoryRef repository,
        string commitSha,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public ValueTask<ScmFileContent?> GetFile(
        ScmConnectionContext context,
        ScmRepositoryRef repository,
        string path,
        string commitSha,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public ValueTask<IReadOnlyList<ScmCodeMatch>> SearchCode(
        ScmConnectionContext context,
        ScmRepositoryRef repository,
        string query,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public ValueTask<IReadOnlyList<ScmPullRequestFile>> ListPullRequestFiles(
        ScmConnectionContext context,
        ScmRepositoryRef repository,
        string number,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public ValueTask<IReadOnlyList<ScmCheckRun>> ListCheckRuns(
        ScmConnectionContext context,
        ScmRepositoryRef repository,
        string commitSha,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    private static async Task<AzureDevOpsCredential> CreateCredential(
        ScmConnectionContext context,
        CancellationToken cancellationToken)
    {
        var accessToken = await context.GetAccessToken(cancellationToken);

        return new AzureDevOpsCredential
        {
            Organisation = context.Connection.AccountLogin,
            Token = accessToken.Token,
        };
    }

    private static void ThrowIfFailed<T>(AzureDevOpsResponse<T> response)
    {
        if (response.IsSuccess)
        {
            return;
        }

        var statusCode = response.Kind == AzureDevOpsResponseKind.Unauthorised
            ? HttpStatusCode.Unauthorized
            : response.StatusCode;

        throw new ScmProviderException(SourceControlProvider.AzureDevOps, (int)statusCode, response.Kind.ToString());
    }
}
