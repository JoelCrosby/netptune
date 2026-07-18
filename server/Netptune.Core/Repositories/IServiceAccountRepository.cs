using Netptune.Core.Entities;
using Netptune.Core.Models.Authentication;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.ServiceAccounts;
using Netptune.Core.Relationships;

namespace Netptune.Core.Repositories;

public interface IServiceAccountRepository : IRepository<ServiceAccount, int>
{
    Task<ApiCredentialAuthentication?> GetCredentialAuthentication(Guid credentialId, CancellationToken cancellationToken = default);

    Task<ServiceAccount?> GetForManagement(int id, int workspaceId, CancellationToken cancellationToken = default);

    Task<List<ServiceAccountViewModel>> GetInWorkspace(int workspaceId, CancellationToken cancellationToken = default);

    Task TouchCredential(Guid credentialId, DateTime usedAt, CancellationToken cancellationToken = default);

    Task AddOwners(IEnumerable<ServiceAccountOwner> owners, CancellationToken cancellationToken = default);

    Task AddCredential(ApiCredential credential, CancellationToken cancellationToken = default);

    Task<ApiCredential?> GetCredentialForManagement(Guid credentialId, int serviceAccountId, int workspaceId, CancellationToken cancellationToken = default);

    Task AddMembership(WorkspaceAppUser membership, CancellationToken cancellationToken = default);
}
