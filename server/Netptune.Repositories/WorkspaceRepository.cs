using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
using Netptune.Core.Enums;
using Netptune.Core.ViewModels.Files;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class WorkspaceRepository : AuditableRepository<DataContext, Workspace, int>, IWorkspaceRepository
{
    public WorkspaceRepository(DataContext context, IDbConnectionFactory connectionFactory)
        : base(context, connectionFactory)
    {
    }

    public async Task DeleteWorkspacePermanent(int workspaceId, CancellationToken cancellationToken = default)
    {
        // ExecuteDeleteAsync runs on the context's own connection, so these all take part in the
        // caller's transaction — either the whole workspace goes or none of it does.
        var taskIds = Context.ProjectTasks
            .Where(task => task.WorkspaceId == workspaceId)
            .Select(task => task.Id);

        var projectIds = Context.Projects
            .Where(project => project.WorkspaceId == workspaceId)
            .Select(project => project.Id);

        var automationRuleIds = Context.AutomationRules
            .Where(rule => rule.WorkspaceId == workspaceId)
            .Select(rule => rule.Id);

        var serviceAccountUserIds = await Context.ServiceAccounts
            .Where(account => account.WorkspaceId == workspaceId)
            .Select(account => account.UserId)
            .ToListAsync(cancellationToken);

        // Notifications and activity entries both point at activity logs, so they go first.
        await Context.Notifications.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);
        await Context.ActivityEntries.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);
        await Context.EventRecords.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);

        // Runs and actions belong to a rule; flags can reference one too.
        await Context.AutomationRuns.Where(x => automationRuleIds.Contains(x.AutomationRuleId)).ExecuteDeleteAsync(cancellationToken);
        await Context.AutomationActions.Where(x => automationRuleIds.Contains(x.AutomationRuleId)).ExecuteDeleteAsync(cancellationToken);
        await Context.Flags.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);
        await Context.AutomationRules.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);

        // Reactions and mentions hang off comments.
        await Context.Reactions.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);
        await Context.CommentMentions.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);
        await Context.Comments.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);

        // Everything joined to a task, then the tasks themselves.
        await Context.ProjectTaskRelations.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);
        await Context.ProjectTaskTags.Where(x => taskIds.Contains(x.ProjectTaskId)).ExecuteDeleteAsync(cancellationToken);
        await Context.ProjectTaskInBoardGroups.Where(x => taskIds.Contains(x.ProjectTaskId)).ExecuteDeleteAsync(cancellationToken);
        await Context.ProjectTaskAppUsers.Where(x => taskIds.Contains(x.ProjectTaskId)).ExecuteDeleteAsync(cancellationToken);
        await Context.ProjectTasks.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);

        // Board groups reference a status; boards and sprints reference a project.
        await Context.BoardGroups.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);
        await Context.Boards.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);
        await Context.Sprints.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);
        await Context.ProjectUsers.Where(x => projectIds.Contains(x.ProjectId)).ExecuteDeleteAsync(cancellationToken);

        // Projects reference their default status, so they go before statuses.
        await Context.Projects.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);
        await Context.Statuses.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);
        await Context.RelationTypes.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);
        await Context.Tags.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);

        // Per-user rows scoped to the workspace, then membership.
        await Context.CommandPaletteRecentItems.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);
        await Context.UserPreferenceValues.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);
        await Context.WorkspaceInvites.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);
        await Context.WorkspaceAppUsers.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);

        await Context.ServiceAccounts.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);
        await Context.AppUsers.Where(x => serviceAccountUserIds.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);

        await Context.Workspaces.Where(x => x.Id == workspaceId).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<int?> GetIdBySlug(string slug, CancellationToken cancellationToken = default)
    {
        var result = await Entities
            .IsReadonly(true)
            .Where(workspace => workspace.Slug == slug && !workspace.IsDeleted)
            .OrderBy(workspace => workspace.Id)
            .Select(x => x.Id)
            .Take(1)
            .ToListAsync(cancellationToken);

        if (result.Any())
        {
            return result.FirstOrDefault();
        }

        return null;
    }

    public Task<Workspace?> GetBySlug(string slug, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        return Entities
            .IsReadonly(isReadonly)
            .FirstOrDefaultAsync(workspace => workspace.Slug == slug && !workspace.IsDeleted, cancellationToken);
    }

    public Task<Workspace?> GetBySlugWithTasks(string slug, bool includeRelated, bool isReadonly = false, CancellationToken cancellationToken = default)
    {

        if (includeRelated)
        {
            return Entities
                .IsReadonly(isReadonly)
                .Include(workspace => workspace.Projects)
                .ThenInclude(project => project.ProjectTasks)
                .FirstOrDefaultAsync(workspace => workspace.Slug == slug && !workspace.IsDeleted, cancellationToken);
        }

        return Entities
            .IsReadonly(isReadonly)
            .FirstOrDefaultAsync(workspace => workspace.Slug == slug && !workspace.IsDeleted, cancellationToken);
    }

    public Task<List<Workspace>> GetUserWorkspaces(string userId, CancellationToken cancellationToken = default, PageRequest? pageRequest = null)
    {
        pageRequest ??= new PageRequest();
        var pagination = pageRequest.GetPagination();

        return Context.WorkspaceAppUsers
            .Where(x => x.UserId == userId)
            .Select(w => w.Workspace)
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Workspace>> GetWorkspaces(CancellationToken cancellationToken = default, PageRequest? pageRequest = null)
    {
        pageRequest ??= new PageRequest();

        var pagination = pageRequest.GetPagination(PaginationDefaults.MaxAdminPageSize);

        return Entities
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<bool> Exists(string slug, CancellationToken cancellationToken = default)
    {
        return Entities.AnyAsync(workspace => workspace.Slug == slug, cancellationToken);
    }

    public Task<Dictionary<int, string>> GetSlugsByIds(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        return Entities
            .IsReadonly(true)
            .Where(w => ids.Contains(w.Id) && !w.IsDeleted)
            .Select(w => new { w.Id, w.Slug })
            .ToDictionaryAsync(w => w.Id, w => w.Slug, cancellationToken);
    }

    public Task<WorkspaceStorageUsageViewModel?> GetStorageUsage(int workspaceId, CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(workspace => workspace.Id == workspaceId)
            .Select(workspace => new WorkspaceStorageUsageViewModel
            {
                UsedBytes = workspace.StorageUsedBytes,
                LimitBytes = workspace.StorageLimitBytes,
                AvailableBytes = Math.Max(0, workspace.StorageLimitBytes - workspace.StorageUsedBytes),
                Percentage = workspace.StorageLimitBytes == 0 ? 100 : workspace.StorageUsedBytes * 100d / workspace.StorageLimitBytes,
                FileCount = workspace.Files.Count(file => !file.IsDeleted && file.Status == WorkspaceFileStatus.Ready),
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> TryReserveStorage(int workspaceId, long sizeBytes, CancellationToken cancellationToken = default)
    {
        var updated = await Entities
            .Where(workspace => workspace.Id == workspaceId &&
                workspace.StorageUsedBytes + sizeBytes <= workspace.StorageLimitBytes)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                workspace => workspace.StorageUsedBytes,
                workspace => workspace.StorageUsedBytes + sizeBytes), cancellationToken);

        return updated > 0;
    }

    public async Task ReleaseStorage(int workspaceId, long sizeBytes, CancellationToken cancellationToken = default)
    {
        await Entities
            .Where(workspace => workspace.Id == workspaceId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                workspace => workspace.StorageUsedBytes,
                workspace => Math.Max(0, workspace.StorageUsedBytes - sizeBytes)), cancellationToken);
    }

    public Task<List<int>> GetAllIds(CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Select(workspace => workspace.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<Workspace?> GetForStorageUpdate(int id, CancellationToken cancellationToken = default)
    {
        return Entities
            .FromSqlInterpolated($"SELECT * FROM workspaces WHERE id = {id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task SetStorageUsage(int id, long sizeBytes, CancellationToken cancellationToken = default)
    {
        await Entities
            .Where(workspace => workspace.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(workspace => workspace.StorageUsedBytes, sizeBytes), cancellationToken);
    }
}
