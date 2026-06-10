using Netptune.Core.Relationships;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface IWorkspaceInviteRepository : IRepository<WorkspaceInvite, int>
{
    Task<WorkspaceInvite?> GetByCode(string code, CancellationToken cancellationToken = default);

    Task<List<WorkspaceInvite>> GetPendingByWorkspace(int workspaceId, CancellationToken cancellationToken = default);

    Task<int> CountPendingByWorkspaceExcludingMembers(int workspaceId, CancellationToken cancellationToken = default);

    Task<List<WorkspaceInvite>> GetPendingByWorkspaceExcludingMembers(
        int workspaceId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<WorkspaceInvite?> GetPendingByEmail(string email, int workspaceId, CancellationToken cancellationToken = default);

    Task<List<WorkspaceInvite>> GetPendingByEmailRange(IEnumerable<string> emails, int workspaceId, CancellationToken cancellationToken = default);

    Task Accept(string code, CancellationToken cancellationToken = default);

    Task DeleteByEmail(string email, int workspaceId, CancellationToken cancellationToken = default);
}
