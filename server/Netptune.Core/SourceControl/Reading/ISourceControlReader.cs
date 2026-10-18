using Netptune.Core.Responses.Common;
using Netptune.Core.SourceControl.Models;

namespace Netptune.Core.SourceControl.Reading;

public interface ISourceControlReader
{
    bool HasProviders { get; }

    Task<List<ScmReadableRepository>> ListRepositories(int workspaceId, CancellationToken cancellationToken);

    Task<ClientResponse<ScmRepositoryFiles>> ListFiles(int workspaceId, string repository, string? reference, CancellationToken cancellationToken);

    Task<ClientResponse<ScmRepositoryFile>> ReadFile(int workspaceId, string repository, string path, string? reference, CancellationToken cancellationToken);

    Task<ClientResponse<IReadOnlyList<ScmCodeMatch>>> SearchCode(int workspaceId, string repository, string query, CancellationToken cancellationToken);

    Task<ClientResponse<IReadOnlyList<ScmCommit>>> ListCommits(int workspaceId, string repository, string? branch, int take, CancellationToken cancellationToken);

    Task<ClientResponse<ScmPullRequestDetail>> GetPullRequest(int workspaceId, string repository, string number, CancellationToken cancellationToken);
}
