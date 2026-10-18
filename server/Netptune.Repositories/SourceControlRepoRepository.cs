using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.History;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class SourceControlRepoRepository(DataContext context, IDbConnectionFactory connectionFactory)
    : Repository<DataContext, SourceControlRepo, int>(context, connectionFactory), ISourceControlRepoRepository
{
    public Task<List<SourceControlRepo>> GetForConnection(
        int connectionId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(repo => repo.ConnectionId == connectionId)
            .ToListAsync(cancellationToken);
    }

    public Task<List<SourceControlRepo>> GetLinkedForWorkspace(
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(repo => repo.WorkspaceId == workspaceId && repo.IsLinked && !repo.Connection.IsDeleted)
            .OrderBy(repo => repo.FullName)
            .ToListAsync(cancellationToken);
    }

    public Task<SourceControlRepo?> GetForHistorySync(int repoId, CancellationToken cancellationToken = default)
    {
        return Entities
            .Include(repo => repo.Connection)
            .Include(repo => repo.Workspace)
            .Where(repo => repo.Id == repoId && !repo.Connection.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> TryClaimHistorySync(int repoId, DateTime now, CancellationToken cancellationToken = default)
    {
        var expiredBefore = now - ScmHistorySyncState.Lease;

        var claimed = await Entities
            .Where(repo => repo.Id == repoId)
            .Where(repo => repo.HistorySyncStartedAt == null || repo.HistorySyncStartedAt < expiredBefore)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(repo => repo.HistorySyncStartedAt, now),
                cancellationToken);

        return claimed == 1;
    }

    public Task ReleaseHistorySync(int repoId, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(repo => repo.Id == repoId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(repo => repo.HistorySyncStartedAt, (DateTime?)null),
                cancellationToken);
    }

    public Task<bool> IsLinked(int repoId, CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .AnyAsync(repo => repo.Id == repoId && repo.IsLinked && repo.IsAccessible, cancellationToken);
    }

    public Task<SourceControlRepo?> GetByExternalId(
        int connectionId,
        string externalId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(repo => repo.ConnectionId == connectionId && repo.ExternalId == externalId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<SourceControlRepo>> GetReadableForWorkspace(
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Include(repo => repo.Connection)
            .Where(repo => repo.WorkspaceId == workspaceId && repo.IsLinked && repo.IsAccessible)
            .Where(repo => !repo.Connection.IsDeleted && repo.Connection.Status == ScmConnectionStatus.Active)
            .OrderBy(repo => repo.FullName)
            .ToListAsync(cancellationToken);
    }

    public Task<SourceControlRepo?> GetOwned(
        int repoId,
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(repo => repo.Id == repoId && repo.WorkspaceId == workspaceId && !repo.Connection.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
