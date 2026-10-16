using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.SourceControl;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class SourceControlConnectionRepository(DataContext context, IDbConnectionFactory connectionFactory)
    : Repository<DataContext, SourceControlConnection, int>(context, connectionFactory), ISourceControlConnectionRepository
{
    public Task<List<SourceControlConnection>> GetForWorkspace(
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(connection => connection.WorkspaceId == workspaceId && !connection.IsDeleted)
            .OrderBy(connection => connection.Provider)
            .ThenBy(connection => connection.AccountLogin)
            .ToListAsync(cancellationToken);
    }

    public Task<SourceControlConnection?> GetOwned(
        int connectionId,
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(connection => connection.Id == connectionId)
            .Where(connection => connection.WorkspaceId == workspaceId && !connection.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<SourceControlConnection?> GetByAccount(
        SourceControlAccountKey key,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(connection => connection.WorkspaceId == key.WorkspaceId && !connection.IsDeleted)
            .Where(connection => connection.Provider == key.Provider)
            .Where(connection => connection.BaseUrl == key.BaseUrl)
            .Where(connection => connection.ExternalAccountId == key.ExternalAccountId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<SourceControlConnection>> GetByExternalAccount(
        SourceControlProvider provider,
        string externalAccountId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(connection => connection.Provider == provider)
            .Where(connection => connection.ExternalAccountId == externalAccountId && !connection.IsDeleted)
            .ToListAsync(cancellationToken);
    }
}
