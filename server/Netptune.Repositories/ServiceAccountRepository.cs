using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Models.Authentication;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.ServiceAccounts;
using Netptune.Core.Relationships;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public sealed class ServiceAccountRepository : Repository<DataContext, ServiceAccount, int>, IServiceAccountRepository
{
    public ServiceAccountRepository(DataContext context, IDbConnectionFactory connectionFactory)
        : base(context, connectionFactory)
    {
    }

    public Task<ApiCredentialAuthentication?> GetCredentialAuthentication(
        Guid credentialId,
        CancellationToken cancellationToken = default)
    {
        return Context.ApiCredentials
            .AsNoTracking()
            .Where(credential => credential.Id == credentialId)
            .Select(credential => new ApiCredentialAuthentication
            {
                CredentialId = credential.Id,
                SecretHash = credential.SecretHash,
                Scopes = credential.Scopes.ToHashSet(),
                ExpiresAt = credential.ExpiresAt,
                RevokedAt = credential.RevokedAt,
                LastUsedAt = credential.LastUsedAt,
                ServiceAccountId = credential.ServiceAccountId,
                UserId = credential.ServiceAccount.UserId,
                DisplayName = credential.ServiceAccount.User.Firstname,
                DisabledAt = credential.ServiceAccount.DisabledAt,
                WorkspaceId = credential.ServiceAccount.WorkspaceId,
                WorkspaceKey = credential.ServiceAccount.Workspace.Slug,
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<ServiceAccount?> GetForManagement(
        int id,
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Include(account => account.User)
            .Include(account => account.Owners)
            .Include(account => account.Credentials)
            .SingleOrDefaultAsync(
                account => account.Id == id && account.WorkspaceId == workspaceId,
                cancellationToken);
    }

    public Task<List<ServiceAccountViewModel>> GetInWorkspace(
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(account => account.WorkspaceId == workspaceId)
            .OrderBy(account => account.User.Firstname)
            .Select(account => new ServiceAccountViewModel
            {
                Id = account.Id,
                UserId = account.UserId,
                Name = account.User.Firstname,
                Description = account.Description,
                CreatedAt = account.CreatedAt,
                DisabledAt = account.DisabledAt,
                OwnerUserIds = account.Owners.Select(owner => owner.UserId).OrderBy(id => id).ToList(),
                Permissions = Context.WorkspaceAppUsers
                    .Where(membership => membership.WorkspaceId == workspaceId && membership.UserId == account.UserId)
                    .Select(membership => membership.Permissions)
                    .Single(),
                Credentials = account.Credentials
                    .OrderByDescending(credential => credential.CreatedAt)
                    .Select(credential => new ApiCredentialViewModel
                    {
                        Id = credential.Id,
                        Name = credential.Name,
                        TokenPrefix = credential.TokenPrefix,
                        CreatedAt = credential.CreatedAt,
                        ExpiresAt = credential.ExpiresAt,
                        RevokedAt = credential.RevokedAt,
                        LastUsedAt = credential.LastUsedAt,
                        Scopes = credential.Scopes,
                    })
                    .ToList(),
            })
            .ToListAsync(cancellationToken);
    }

    public Task TouchCredential(Guid credentialId, DateTime usedAt, CancellationToken cancellationToken = default)
    {
        return Context.ApiCredentials
            .Where(credential => credential.Id == credentialId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(credential => credential.LastUsedAt, usedAt),
                cancellationToken);
    }

    public Task AddOwners(IEnumerable<ServiceAccountOwner> owners, CancellationToken cancellationToken = default)
    {
        return Context.ServiceAccountOwners.AddRangeAsync(owners, cancellationToken);
    }

    public async Task AddCredential(ApiCredential credential, CancellationToken cancellationToken = default)
    {
        await Context.ApiCredentials.AddAsync(credential, cancellationToken);
    }

    public Task<ApiCredential?> GetCredentialForManagement(
        Guid credentialId,
        int serviceAccountId,
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        return Context.ApiCredentials
            .Include(credential => credential.ServiceAccount)
            .SingleOrDefaultAsync(
                credential => credential.Id == credentialId
                              && credential.ServiceAccountId == serviceAccountId
                              && credential.ServiceAccount.WorkspaceId == workspaceId,
                cancellationToken);
    }

    public async Task AddMembership(WorkspaceAppUser membership, CancellationToken cancellationToken = default)
    {
        await Context.WorkspaceAppUsers.AddAsync(membership, cancellationToken);
    }
}
