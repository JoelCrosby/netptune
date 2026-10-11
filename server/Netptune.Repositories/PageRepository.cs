using System.Linq.Expressions;

using Dapper;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Models.Pages;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Pages;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;
using Netptune.Repositories.RowMaps;
using Netptune.Repositories.Sql;

namespace Netptune.Repositories;

public sealed class PageRepository : WorkspaceEntityRepository<DataContext, Page, int>, IPageRepository
{
    public PageRepository(DataContext context, IDbConnectionFactory connectionFactory) : base(context, connectionFactory) { }

    public Task<Page?> GetLiveInWorkspace(int id, int workspaceId, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        return Entities
            .IsReadonly(isReadonly)
            .Where(page => page.Id == id && page.WorkspaceId == workspaceId && !page.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<Page?> GetLiveWithTags(int id, int workspaceId, CancellationToken cancellationToken = default)
    {
        return Entities
            .Include(page => page.PageTags)
            .Where(page => page.Id == id && page.WorkspaceId == workspaceId && !page.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<Page?> GetArchivedInWorkspace(int id, int workspaceId, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(page => page.Id == id && page.WorkspaceId == workspaceId && page.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<List<PageTreeNodeViewModel>> GetTree(int workspaceId, string userId, CancellationToken cancellationToken = default)
    {
        var favourites = Context.Set<PageFavourite>();

        return Entities
            .AsNoTracking()
            .Where(page => page.WorkspaceId == workspaceId && !page.IsDeleted && !page.IsTemplate)
            .OrderBy(page => page.SortOrder)
            .ThenBy(page => page.Id)
            .Take(PageTree.MaxNodes)
            .Select(page => new PageTreeNodeViewModel
            {
                Id = page.Id,
                ParentId = page.ParentId,
                Kind = page.Kind,
                Title = page.Title,
                Slug = page.Slug,
                Icon = page.Icon,
                SortOrder = page.SortOrder,
                Status = page.Status,
                IsFavourite = favourites.Any(favourite => favourite.PageId == page.Id && favourite.UserId == userId),
            })
            .ToListAsync(cancellationToken);
    }

    public Task<PageViewModel?> GetViewModel(int id, int workspaceId, string userId, CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(page => page.Id == id && page.WorkspaceId == workspaceId && !page.IsDeleted)
            .Select(ToViewModel(userId))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<PageViewModel?> GetViewModelBySlug(string slug, int workspaceId, string userId, CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(page => page.Slug == slug && page.WorkspaceId == workspaceId && !page.IsDeleted)
            .Select(ToViewModel(userId))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<List<ArchivedPageViewModel>> GetArchived(int workspaceId, CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(page => page.WorkspaceId == workspaceId && page.IsDeleted && !page.IsTemplate)
            .Where(page => page.Parent == null || !page.Parent.IsDeleted)
            .OrderByDescending(page => page.UpdatedAt)
            .ThenByDescending(page => page.Id)
            .Take(PageTree.MaxNodes)
            .Select(page => new ArchivedPageViewModel
            {
                Id = page.Id,
                Kind = page.Kind,
                Title = page.Title,
                Slug = page.Slug,
                ArchivedAt = page.UpdatedAt,
                ArchivedByDisplayName = page.DeletedByUser == null ? null : page.DeletedByUser.DisplayName,
                CreatedByUserId = page.CreatedByUserId,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<double> GetNextSortOrder(int workspaceId, int? parentId, CancellationToken cancellationToken = default)
    {
        var lastSortOrder = await Entities
            .Where(page => page.WorkspaceId == workspaceId && page.ParentId == parentId && !page.IsDeleted)
            .MaxAsync(page => (double?)page.SortOrder, cancellationToken);

        return (lastSortOrder ?? 0) + 1;
    }

    public async Task<bool> TrySaveContent(PageContentUpdate update, CancellationToken cancellationToken = default)
    {
        var affected = await Entities
            .Where(page => page.Id == update.PageId && page.Revision == update.ExpectedRevision && !page.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(page => page.Title, update.Title)
                .SetProperty(page => page.Body, update.Body)
                .SetProperty(page => page.Revision, page => page.Revision + 1)
                .SetProperty(page => page.ModifiedByUserId, update.UserId)
                .SetProperty(page => page.UpdatedAt, DateTime.UtcNow), cancellationToken);

        return affected == 1;
    }

    public async Task<List<int>> GetSubtreeIds(int pageId, int workspaceId, bool includeArchived, CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.StartConnection();

        var command = new CommandDefinition(
            SqlScripts.GetPageSubtreeIds,
            new { PageId = pageId, WorkspaceId = workspaceId, IncludeArchived = includeArchived },
            cancellationToken: cancellationToken);

        var ids = await connection.QueryAsync<int>(command);

        return ids.ToList();
    }

    public async Task<List<int>> GetArchivedTogetherIds(Page root, CancellationToken cancellationToken = default)
    {
        var subtreeIds = await GetSubtreeIds(root.Id, root.WorkspaceId, true, cancellationToken);

        // Archiving stamps one UpdatedAt and DeletedByUserId on the whole subtree, which tells
        // the pages archived with the root apart from ones archived on their own beforehand.
        return await Entities
            .Where(page => subtreeIds.Contains(page.Id) && page.WorkspaceId == root.WorkspaceId && page.IsDeleted)
            .Where(page => page.UpdatedAt == root.UpdatedAt && page.DeletedByUserId == root.DeletedByUserId)
            .Select(page => page.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> AnyCreatedByOthers(IReadOnlyCollection<int> ids, string userId, CancellationToken cancellationToken = default)
    {
        return Entities.AnyAsync(page => ids.Contains(page.Id) && page.CreatedByUserId != userId, cancellationToken);
    }

    public async Task<List<int>> GetAncestorIds(int pageId, int workspaceId, CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.StartConnection();

        var command = new CommandDefinition(
            SqlScripts.GetPageAncestorIds,
            new { PageId = pageId, WorkspaceId = workspaceId },
            cancellationToken: cancellationToken);

        var ids = await connection.QueryAsync<int>(command);

        return ids.ToList();
    }

    public Task<List<PageSearchSource>> GetSearchSources(IReadOnlyCollection<int> ids, string workspaceKey, CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(page => ids.Contains(page.Id) && page.Workspace!.Slug == workspaceKey && !page.IsDeleted && !page.IsTemplate)
            .Select(ToSearchSource)
            .ToListAsync(cancellationToken);
    }

    public Task<List<PageSearchSource>> GetAllSearchSources(string workspaceKey, CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(page => page.Workspace!.Slug == workspaceKey && !page.IsDeleted && !page.IsTemplate)
            .Select(ToSearchSource)
            .ToListAsync(cancellationToken);
    }

    public Task<List<PageOutlineEntry>> GetOutline(string workspaceKey, CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(page => page.Workspace!.Slug == workspaceKey && !page.IsDeleted && !page.IsTemplate)
            .Select(page => new PageOutlineEntry(page.Id, page.ParentId, page.Title))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PageOutlineEntry>> GetAncestorOutline(IReadOnlyCollection<int> pageIds, string workspaceKey, CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.StartConnection();

        var command = new CommandDefinition(
            SqlScripts.GetPageAncestorOutline,
            new { PageIds = pageIds.ToArray(), WorkspaceKey = workspaceKey },
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<PageOutlineRow>(command);

        return rows.Select(row => new PageOutlineEntry(row.Id, row.Parent_id, row.Title)).ToList();
    }

    public Task<List<int>> GetIdsTaggedWith(IReadOnlyCollection<int> tagIds, int workspaceId, CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(page => page.WorkspaceId == workspaceId && !page.IsDeleted && !page.IsTemplate)
            .Where(page => page.PageTags.Any(pageTag => tagIds.Contains(pageTag.TagId)))
            .Select(page => page.Id)
            .ToListAsync(cancellationToken);
    }

    private static readonly Expression<Func<Page, PageSearchSource>> ToSearchSource = page => new PageSearchSource
    {
        Id = page.Id,
        ParentId = page.ParentId,
        Kind = page.Kind,
        Title = page.Title,
        Slug = page.Slug,
        Body = page.Body,
        Status = page.Status,
        ProjectId = page.ProjectId,
        Tags = page.PageTags
            .Where(pageTag => pageTag.Tag != null && !pageTag.Tag.IsDeleted)
            .Select(pageTag => pageTag.Tag!.Name)
            .ToList(),
        UpdatedAt = page.UpdatedAt ?? page.CreatedAt,
    };

    private Expression<Func<Page, PageViewModel>> ToViewModel(string userId)
    {
        var favourites = Context.Set<PageFavourite>();

        return page => new PageViewModel
        {
            Id = page.Id,
            ParentId = page.ParentId,
            ProjectId = page.ProjectId,
            Kind = page.Kind,
            Title = page.Title,
            Slug = page.Slug,
            Icon = page.Icon,
            Body = page.Body,
            Status = page.Status,
            Revision = page.Revision,
            IsFavourite = favourites.Any(favourite => favourite.PageId == page.Id && favourite.UserId == userId),
            CreatedAt = page.CreatedAt,
            UpdatedAt = page.UpdatedAt,
            CreatedByUserId = page.CreatedByUserId,
            CreatedByDisplayName = page.CreatedByUser == null ? null : page.CreatedByUser.DisplayName,
            ModifiedByDisplayName = page.ModifiedByUser == null ? null : page.ModifiedByUser.DisplayName,
            Tags = page.PageTags
                .Where(pageTag => !pageTag.Tag!.IsDeleted)
                .OrderBy(pageTag => pageTag.Tag!.Name)
                .Select(pageTag => new PageTagViewModel { Id = pageTag.TagId, Name = pageTag.Tag!.Name })
                .ToList(),
        };
    }
}
