using Microsoft.EntityFrameworkCore;

using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
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

    public async Task<PagedResponse<RelationTypeViewModel>> GetPageForWorkspace(int workspaceId, RelationTypeFilter filter, CancellationToken cancellationToken = default)
    {
        var query = Entities.Where(relationType => relationType.WorkspaceId == workspaceId && !relationType.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();

            query = query.Where(relationType =>
                EF.Functions.ILike(relationType.Name, $"%{search}%") ||
                EF.Functions.ILike(relationType.InverseName, $"%{search}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var pagination = filter.GetPagination();
        var items = await SortRelationTypes(Project(query), filter)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<RelationTypeViewModel>(items, pagination.Page, pagination.PageSize, totalCount);
    }

    private static IQueryable<RelationTypeViewModel> Project(IQueryable<RelationType> query)
    {
        return query
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
                RelationCount = relationType.ProjectTaskRelations.Count(),
            });
    }

    private static IQueryable<RelationTypeViewModel> SortRelationTypes(IQueryable<RelationTypeViewModel> query, RelationTypeFilter filter)
    {
        var isDescending = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return filter.SortBy?.ToLowerInvariant() switch
        {
            "name" => isDescending
                ? query.OrderByDescending(relationType => relationType.Name).ThenBy(relationType => relationType.Id)
                : query.OrderBy(relationType => relationType.Name).ThenBy(relationType => relationType.Id),
            "inversename" => isDescending
                ? query.OrderByDescending(relationType => relationType.InverseName).ThenBy(relationType => relationType.Name)
                : query.OrderBy(relationType => relationType.InverseName).ThenBy(relationType => relationType.Name),
            "category" => isDescending
                ? query.OrderByDescending(relationType => relationType.Category).ThenBy(relationType => relationType.SortOrder)
                : query.OrderBy(relationType => relationType.Category).ThenBy(relationType => relationType.SortOrder),
            "relationcount" => isDescending
                ? query.OrderByDescending(relationType => relationType.RelationCount).ThenBy(relationType => relationType.SortOrder)
                : query.OrderBy(relationType => relationType.RelationCount).ThenBy(relationType => relationType.SortOrder),
            _ => isDescending
                ? query.OrderByDescending(relationType => relationType.SortOrder).ThenByDescending(relationType => relationType.Id)
                : query.OrderBy(relationType => relationType.SortOrder).ThenBy(relationType => relationType.Id),
        };
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

    public static string BuildKey(string name)
    {
        var key = name.Trim().ToUrlSlug();
        return string.IsNullOrWhiteSpace(key) ? Guid.NewGuid().ToString("N")[..8] : key;
    }
}
