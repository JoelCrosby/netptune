using Netptune.Transfer.Repositories;
using Netptune.Transfer.Entities;
using Microsoft.EntityFrameworkCore;

using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public sealed class ExportDefinitionRepository : WorkspaceEntityRepository<DataContext, ExportDefinition, int>, IExportDefinitionRepository
{
    public ExportDefinitionRepository(DataContext context, IDbConnectionFactory connectionFactory) : base(context, connectionFactory) { }

    public Task<List<ExportDefinition>> GetVisibleInWorkspace(int workspaceId, string currentUserId, CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Include(definition => definition.CreatedByUser)
            .Where(definition => definition.WorkspaceId == workspaceId && !definition.IsDeleted)
            .Where(definition => definition.IsShared || definition.CreatedByUserId == currentUserId)
            .OrderBy(definition => definition.Name)
            .ToListAsync(cancellationToken);
    }

    public override Task<ExportDefinition?> GetInWorkspace(int id, int workspaceId, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        var query = Entities.Where(definition => definition.Id == id && definition.WorkspaceId == workspaceId && !definition.IsDeleted);

        if (isReadonly)
        {
            return query.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        }

        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> NameExists(int workspaceId, string name, int? excludeId, CancellationToken cancellationToken = default)
    {
        return Entities.AnyAsync(
            definition => definition.WorkspaceId == workspaceId &&
                !definition.IsDeleted &&
                definition.Name.ToLower() == name.ToLower() &&
                (excludeId == null || definition.Id != excludeId),
            cancellationToken);
    }
}
