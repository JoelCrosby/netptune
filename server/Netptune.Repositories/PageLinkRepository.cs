using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
using Netptune.Core.ViewModels.Pages;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public sealed class PageLinkRepository(DataContext context, IDbConnectionFactory connectionFactory)
    : Repository<DataContext, PageLink, int>(context, connectionFactory), IPageLinkRepository
{
    // The delete runs now and the inserts are staged, so both belong inside the caller's transaction.
    public async Task ReplaceExtracted(int pageId, IReadOnlyCollection<PageLink> links, CancellationToken cancellationToken = default)
    {
        await Entities
            .Where(link => link.PageId == pageId && !link.IsExplicit)
            .ExecuteDeleteAsync(cancellationToken);

        await Entities.AddRangeAsync(links, cancellationToken);
    }

    public async Task<List<PageLinkedTaskViewModel>> GetLinkedTasks(int pageId, int workspaceId, CancellationToken cancellationToken = default)
    {
        var taskIds = await GetTargetIds(pageId, PageLinkTargetType.Task, cancellationToken);

        if (taskIds.Count == 0)
        {
            return [];
        }

        return await Context.Set<ProjectTask>()
            .AsNoTracking()
            .Where(task => taskIds.Contains(task.Id) && task.WorkspaceId == workspaceId && !task.IsDeleted)
            .OrderBy(task => task.Name)
            .Select(task => new PageLinkedTaskViewModel
            {
                Id = task.Id,
                SystemId = task.Project == null
                    ? task.ProjectScopeId.ToString()
                    : task.Project.Key + "-" + task.ProjectScopeId.ToString(),
                Name = task.Name,
                StatusName = task.Status == null || task.Status.IsDeleted ? null : task.Status.Name,
                StatusColor = task.Status == null || task.Status.IsDeleted ? null : task.Status.Color,
                StatusCategory = task.Status == null || task.Status.IsDeleted ? null : task.Status.Category,
                SprintName = task.Sprint == null || task.Sprint.IsDeleted ? null : task.Sprint.Name,
                AssigneeNames = task.ProjectTaskAppUsers.Select(assignee => assignee.User.DisplayName).ToList(),
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PageLinkedPageViewModel>> GetLinkedPages(int pageId, int workspaceId, CancellationToken cancellationToken = default)
    {
        var pageIds = await GetTargetIds(pageId, PageLinkTargetType.Page, cancellationToken);

        if (pageIds.Count == 0)
        {
            return [];
        }

        return await Context.Set<Page>()
            .AsNoTracking()
            .Where(page => pageIds.Contains(page.Id) && page.WorkspaceId == workspaceId && !page.IsDeleted)
            .OrderBy(page => page.Title)
            .Select(page => new PageLinkedPageViewModel
            {
                Id = page.Id,
                Title = page.Title,
                Slug = page.Slug,
            })
            .ToListAsync(cancellationToken);
    }

    public Task<List<PageBacklinkViewModel>> GetBacklinks(
        PageLinkTargetType targetType,
        string targetId,
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(link => link.TargetType == targetType && link.TargetId == targetId)
            .Where(link => link.Page!.WorkspaceId == workspaceId && !link.Page.IsDeleted)
            .OrderByDescending(link => link.Page!.UpdatedAt ?? link.Page.CreatedAt)
            .Take(PaginationDefaults.MaxUnpagedRows)
            .Select(link => new PageBacklinkViewModel
            {
                PageId = link.PageId,
                Title = link.Page!.Title,
                Slug = link.Page.Slug,
                Snippet = link.Snippet,
            })
            .ToListAsync(cancellationToken);
    }

    // Target ids are stored as text so user ids fit the same column; numeric targets are parsed here so
    // the entity lookups can use their primary keys.
    private async Task<List<int>> GetTargetIds(int pageId, PageLinkTargetType targetType, CancellationToken cancellationToken)
    {
        var targetIds = await Entities
            .AsNoTracking()
            .Where(link => link.PageId == pageId && link.TargetType == targetType)
            .Select(link => link.TargetId)
            .Take(PaginationDefaults.MaxUnpagedRows)
            .ToListAsync(cancellationToken);

        return targetIds
            .Select(targetId => int.TryParse(targetId, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToList();
    }
}
