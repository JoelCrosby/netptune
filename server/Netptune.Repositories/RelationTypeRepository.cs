using Microsoft.EntityFrameworkCore;

using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Relations;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.RelationTypes;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public sealed class RelationTypeRepository : WorkspaceEntityRepository<DataContext, RelationType, int>, IRelationTypeRepository
{
    public RelationTypeRepository(DataContext context, IDbConnectionFactory connectionFactory)
        : base(context, connectionFactory)
    {
    }

    public Task<List<RelationTypeViewModel>> GetViewModelsForWorkspace(int workspaceId, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(relationType => relationType.WorkspaceId == workspaceId && !relationType.IsDeleted)
            .OrderBy(relationType => relationType.SortOrder)
            .ThenBy(relationType => relationType.Id)
            .AsNoTracking()
            .Select(relationType => new RelationTypeViewModel
            {
                Id = relationType.Id,
                WorkspaceId = relationType.WorkspaceId,
                Name = relationType.Name,
                InverseName = relationType.InverseName,
                Key = relationType.Key,
                Description = relationType.Description,
                Color = relationType.Color,
                SortOrder = relationType.SortOrder,
                Category = relationType.Category,
                IsSystem = relationType.IsSystem,
            })
            .ToListAsync(cancellationToken);
    }

    public Task<RelationTypeViewModel?> GetViewModel(int id, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(relationType => relationType.Id == id && !relationType.IsDeleted)
            .AsNoTracking()
            .Select(relationType => new RelationTypeViewModel
            {
                Id = relationType.Id,
                WorkspaceId = relationType.WorkspaceId,
                Name = relationType.Name,
                InverseName = relationType.InverseName,
                Key = relationType.Key,
                Description = relationType.Description,
                Color = relationType.Color,
                SortOrder = relationType.SortOrder,
                Category = relationType.Category,
                IsSystem = relationType.IsSystem,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<RelationType?> GetInWorkspace(int id, int workspaceId, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(relationType => relationType.Id == id && relationType.WorkspaceId == workspaceId && !relationType.IsDeleted)
            .IsReadonly(isReadonly)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> KeyExists(int workspaceId, string key, int? excludingId = null, CancellationToken cancellationToken = default)
    {
        return Entities.AnyAsync(relationType =>
            relationType.WorkspaceId == workspaceId &&
            relationType.Key == key &&
            !relationType.IsDeleted &&
            (!excludingId.HasValue || relationType.Id != excludingId.Value), cancellationToken);
    }

    public Task<bool> IsInUse(int relationTypeId, CancellationToken cancellationToken = default)
    {
        return Context.ProjectTaskRelations.AnyAsync(relation => relation.RelationTypeId == relationTypeId, cancellationToken);
    }

    public async Task EnsureDefaultRelationTypes(int workspaceId, string? ownerId, CancellationToken cancellationToken = default)
    {
        var existingKeys = await Entities
            .Where(relationType => relationType.WorkspaceId == workspaceId)
            .Select(relationType => relationType.Key)
            .ToListAsync(cancellationToken);

        var existingSet = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = DefaultRelationTypes.All
            .Where(definition => !existingSet.Contains(definition.Key))
            .Select(definition => DefaultRelationTypes.Create(definition, workspaceId, ownerId))
            .ToList();

        if (missing.Count == 0) return;

        await Entities.AddRangeAsync(missing, cancellationToken);
    }

    public static string BuildKey(string name)
    {
        var key = name.Trim().ToUrlSlug();
        return string.IsNullOrWhiteSpace(key) ? Guid.NewGuid().ToString("N")[..8] : key;
    }
}
