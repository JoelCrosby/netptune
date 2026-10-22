using System.Linq.Expressions;

using Dapper;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Pages;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Pages;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;
using Netptune.Repositories.RowMaps;
using Netptune.Repositories.Sql;

namespace Netptune.Repositories;

public sealed class PageRepository : WorkspaceEntityRepository<DataContext, Page, int>, IPageRepository
{
    private const int PageContributorLimit = 20;

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

    public Task<List<PageTreeNodeViewModel>> GetTree(int workspaceId, string? userId, CancellationToken cancellationToken = default)
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
                ProjectId = page.ProjectId,
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

    public Task<List<PageVisibilityNode>> GetVisibilityNodes(int workspaceId, CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(page => page.WorkspaceId == workspaceId && !page.IsDeleted && !page.IsTemplate)
            .Select(page => new PageVisibilityNode
            {
                Id = page.Id,
                ParentId = page.ParentId,
                Kind = page.Kind,
                Status = page.Status,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PageViewModel?> GetViewModel(int id, int workspaceId, string? userId, CancellationToken cancellationToken = default)
    {
        var page = await Entities
            .AsNoTracking()
            .Where(page => page.Id == id && page.WorkspaceId == workspaceId && !page.IsDeleted)
            .Select(ToViewModel(userId))
            .SingleOrDefaultAsync(cancellationToken);

        return await WithContributors(page, cancellationToken);
    }

    public async Task<PageViewModel?> GetViewModelBySlug(string slug, int workspaceId, string? userId, CancellationToken cancellationToken = default)
    {
        var exact = await Entities
            .AsNoTracking()
            .Where(page => page.Slug == slug && page.WorkspaceId == workspaceId && !page.IsDeleted)
            .Select(ToViewModel(userId))
            .SingleOrDefaultAsync(cancellationToken);

        var id = PageSlug.IdOf(slug);

        if (exact is not null || id is null)
        {
            return await WithContributors(exact, cancellationToken);
        }

        var idSuffix = $"-{id}";
        var renamed = await Entities
            .AsNoTracking()
            .Where(page => page.Slug.EndsWith(idSuffix) && page.WorkspaceId == workspaceId && !page.IsDeleted)
            .OrderBy(page => page.Id)
            .Select(ToViewModel(userId))
            .FirstOrDefaultAsync(cancellationToken);

        return await WithContributors(renamed, cancellationToken);
    }

    private async Task<PageViewModel?> WithContributors(PageViewModel? page, CancellationToken cancellationToken)
    {
        if (page is null)
        {
            return null;
        }

        var contributors = await Context.Set<PageVersion>()
            .AsNoTracking()
            .Where(version => version.PageId == page.Id)
            .GroupBy(version => new
            {
                version.CreatedByUserId,
                version.CreatedByUser!.DisplayName,
                version.CreatedByUser.PictureUrl,
            })
            .Select(group => new PageContributorViewModel
            {
                UserId = group.Key.CreatedByUserId,
                DisplayName = group.Key.DisplayName,
                PictureUrl = group.Key.PictureUrl,
                LastEditedAt = group.Max(version => version.CreatedAt),
            })
            .OrderByDescending(contributor => contributor.LastEditedAt)
            .Take(PageContributorLimit)
            .ToListAsync(cancellationToken);

        return page with { Contributors = contributors };
    }

    public Task<List<PageBodyViewModel>> GetBodies(int workspaceId, IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(page => ids.Contains(page.Id) && page.WorkspaceId == workspaceId && !page.IsDeleted && !page.IsTemplate)
            .Select(page => new PageBodyViewModel(page.Id, page.Body ?? string.Empty))
            .ToListAsync(cancellationToken);
    }

    public Task<PagedResponse<PageBodyViewModel>> FindByText(int workspaceId, PageTextSearch search, CancellationToken cancellationToken = default)
    {
        var matchesBody = TextMatch.On<Page>(page => page.Body, search.Pattern);
        var query = Entities
            .AsNoTracking()
            .Where(page => page.WorkspaceId == workspaceId && !page.IsDeleted && !page.IsTemplate)
            .Where(matchesBody);

        if (search.ScopeIds is not null)
        {
            var scopeIds = search.ScopeIds;

            query = query.Where(page => scopeIds.Contains(page.Id));
        }

        var skip = (search.Page - 1) * search.PageSize;

        return TextMatch.RunPatternQuery(Context, async () =>
        {
            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderBy(page => page.Title)
                .ThenBy(page => page.Id)
                .Skip(skip)
                .Take(search.PageSize)
                .Select(page => new PageBodyViewModel(page.Id, page.Body ?? string.Empty))
                .ToListAsync(cancellationToken);

            return new PagedResponse<PageBodyViewModel>(items, search.Page, search.PageSize, total);
        });
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
                .SetProperty(page => page.Slug, update.Slug)
                .SetProperty(page => page.Body, update.Body)
                .SetProperty(page => page.Revision, page => page.Revision + 1)
                .SetProperty(page => page.ModifiedByUserId, update.UserId)
                .SetProperty(page => page.UpdatedAt, DateTime.UtcNow), cancellationToken);

        return affected == 1;
    }

    public async Task<List<int>> DeleteArchived(IReadOnlyCollection<int> ids, int workspaceId, CancellationToken cancellationToken = default)
    {
        var archivedIds = await Entities
            .Where(page => ids.Contains(page.Id) && page.WorkspaceId == workspaceId && page.IsDeleted)
            .Select(page => page.Id)
            .ToListAsync(cancellationToken);

        // The parent key restricts deletes, so links into the deleted pages are cut first, both within the
        // subtree and from any page outside it that still sits underneath.
        await Entities
            .Where(page => page.ParentId != null && archivedIds.Contains(page.ParentId.Value))
            .ExecuteUpdateAsync(setters => setters.SetProperty(page => page.ParentId, (int?)null), cancellationToken);

        await Entities
            .Where(page => archivedIds.Contains(page.Id))
            .ExecuteDeleteAsync(cancellationToken);

        return archivedIds;
    }

    public Task<bool> AnyLiveInWorkspace(int workspaceId, CancellationToken cancellationToken = default)
    {
        return Entities.AnyAsync(page => page.WorkspaceId == workspaceId && !page.IsDeleted, cancellationToken);
    }

    public async Task<HashSet<string>> GetSlugs(int workspaceId, CancellationToken cancellationToken = default)
    {
        var slugs = await Entities
            .AsNoTracking()
            .Where(page => page.WorkspaceId == workspaceId)
            .Select(page => page.Slug)
            .ToListAsync(cancellationToken);

        return [.. slugs];
    }

    public async Task<bool> MarkReviewed(int id, int workspaceId, string userId, CancellationToken cancellationToken = default)
    {
        var affected = await Entities
            .Where(page => page.Id == id && page.WorkspaceId == workspaceId && !page.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(page => page.ReviewedAt, DateTime.UtcNow)
                .SetProperty(page => page.ReviewedByUserId, userId), cancellationToken);

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

    public Task<List<RecentPageViewModel>> GetRecentlyVisited(int workspaceId, string userId, int take, CancellationToken cancellationToken = default)
    {
        var pages = Entities.Where(page => page.WorkspaceId == workspaceId && !page.IsDeleted && !page.IsTemplate);

        return Context.Set<PageVisit>()
            .AsNoTracking()
            .Where(visit => visit.UserId == userId)
            .Join(pages, visit => visit.PageId, page => page.Id, (visit, page) => new { visit, page })
            .OrderByDescending(pair => pair.visit.VisitedAt)
            .Take(take)
            .Select(pair => new RecentPageViewModel
            {
                Id = pair.page.Id,
                ParentId = pair.page.ParentId,
                Title = pair.page.Title,
                Slug = pair.page.Slug,
                VisitedAt = pair.visit.VisitedAt,
                UpdatedAt = pair.page.UpdatedAt ?? pair.page.CreatedAt,
                UpdatedByDisplayName = pair.page.ModifiedByUser == null
                    ? pair.page.CreatedByUser == null ? null : pair.page.CreatedByUser.DisplayName
                    : pair.page.ModifiedByUser.DisplayName,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PageNeedingReviewViewModel>> GetNeedingReview(PageReviewWindow window, CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.StartConnection();

        var parameters = new
        {
            window.WorkspaceId,
            window.StaleBefore,
            window.UnvisitedBefore,
            window.Take,
        };
        var command = new CommandDefinition(SqlScripts.GetPagesNeedingReview, parameters, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<PageNeedingReviewRow>(command);

        return rows
            .Select(row => new PageNeedingReviewViewModel
            {
                Id = row.Id,
                ParentId = row.Parent_id,
                Title = row.Title,
                Slug = row.Slug,
                UpdatedAt = row.Updated_at,
                Reason = (PageReviewReason)row.Reason,
            })
            .ToList();
    }

    public async Task<List<PageChildViewModel>> GetChildren(int parentId, int workspaceId, CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.StartConnection();

        var parameters = new
        {
            ParentId = parentId,
            WorkspaceId = workspaceId,
            PageSummary.ExcerptSourceLength,
        };
        var command = new CommandDefinition(SqlScripts.GetPageChildren, parameters, cancellationToken: cancellationToken);
        var results = await connection.QueryMultipleAsync(command);
        var rows = await results.ReadAsync<PageChildRow>();
        var tagRows = await results.ReadAsync<PageChildTagRow>();
        var tagsByPage = tagRows.ToLookup(
            row => row.Page_id,
            row => new PageTagViewModel { Id = row.Id, Name = row.Name });

        return rows.Select(row => ToChildViewModel(row, tagsByPage[row.Id])).ToList();
    }

    private static PageChildViewModel ToChildViewModel(PageChildRow row, IEnumerable<PageTagViewModel> tags)
    {
        return new PageChildViewModel
        {
            Id = row.Id,
            Kind = (PageKind)row.Kind,
            Title = row.Title,
            Slug = row.Slug,
            Status = (PageStatus)row.Status,
            Excerpt = PageSummary.Excerpt(row.Excerpt_source, row.Is_body_truncated),
            Tags = tags.ToList(),
            UpdatedAt = row.Updated_at,
            EditorDisplayName = row.Editor_display_name,
            EditorPictureUrl = row.Editor_picture_url,
            ReadMinutes = PageSummary.ReadMinutes(row.Word_count),
            ChildCount = (int)row.Child_count,
        };
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

    private Expression<Func<Page, PageViewModel>> ToViewModel(string? userId)
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
