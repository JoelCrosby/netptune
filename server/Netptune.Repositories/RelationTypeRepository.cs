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

    public async Task<List<RelationTypeViewModel>> GetViewModelsForWorkspace(int workspaceId, CancellationToken cancellationToken = default)
    {
        var relationTypes = await Entities
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

        var relationCounts = await GetRelationCounts(workspaceId, cancellationToken);

        return relationTypes.ConvertAll(relationType => relationType with
        {
            RelationCount = relationCounts.GetValueOrDefault(relationType.Id),
        });
    }

    public Task<Dictionary<int, int>> GetRelationCounts(int workspaceId, CancellationToken cancellationToken = default)
    {
        return Context.ProjectTaskRelations
            .AsNoTracking()
            .Where(relation => relation.WorkspaceId == workspaceId)
            .GroupBy(relation => relation.RelationTypeId)
            .Select(group => new { RelationTypeId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.RelationTypeId, row => row.Count, cancellationToken);
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

    public override Task<RelationType?> GetInWorkspace(int id, int workspaceId, bool isReadonly = false, CancellationToken cancellationToken = default)
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

    public Task<int> GetRelationCount(int relationTypeId, CancellationToken cancellationToken = default)
    {
        return Context.ProjectTaskRelations
            .AsNoTracking()
            .CountAsync(relation => relation.RelationTypeId == relationTypeId, cancellationToken);
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
