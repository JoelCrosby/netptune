using Netptune.Core.Responses.Common;
using Netptune.Core.SourceControl.Models;

namespace Netptune.Core.SourceControl;

public interface ISourceControlClient
{
    IAsyncEnumerable<ScmRepository> ListRepositories(ScmConnectionContext context, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<ScmBranch>> ListBranches(ScmConnectionContext context, ScmRepositoryRef repository, CancellationToken cancellationToken);

    ValueTask<ScmBranch?> GetBranch(ScmConnectionContext context, ScmRepositoryRef repository, string name, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<ScmCommit>> ListCommits(ScmConnectionContext context, ScmRepositoryRef repository, ScmCommitQuery query, CancellationToken cancellationToken);

    ValueTask<ScmPullRequest?> GetPullRequest(ScmConnectionContext context, ScmRepositoryRef repository, string number, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<ScmPullRequest>> ListPullRequests(ScmConnectionContext context, ScmRepositoryRef repository, ScmPullRequestQuery query, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<ScmCheckRun>> ListCheckRuns(ScmConnectionContext context, ScmRepositoryRef repository, string commitSha, CancellationToken cancellationToken);

    ValueTask<ClientResponse<ScmBranch>> CreateBranch(ScmConnectionContext context, ScmRepositoryRef repository, ScmCreateBranch request, CancellationToken cancellationToken);
}
