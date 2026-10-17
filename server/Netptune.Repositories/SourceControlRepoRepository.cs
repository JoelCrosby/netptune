using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
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

    public Task<SourceControlRepo?> GetByExternalId(
        int connectionId,
        string externalId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(repo => repo.ConnectionId == connectionId && repo.ExternalId == externalId)
            .FirstOrDefaultAsync(cancellationToken);
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
