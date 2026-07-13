using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
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

        // Notifications and activity entries both point at activity logs, so they go first.
        await Context.Notifications.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);
        await Context.ActivityEntries.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);
        await Context.ActivityLogs.Where(x => x.WorkspaceId == workspaceId).ExecuteDeleteAsync(cancellationToken);

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

        await Context.Workspaces.Where(x => x.Id == workspaceId).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<int?> GetIdBySlug(string slug, CancellationToken cancellationToken = default)
    {
        var result = await Entities
            .IsReadonly(true)
            .Where(workspace => workspace.Slug == slug && !workspace.IsDeleted)
            .Select(x => x.Id)
            .Take(1)
            .ToListAsync(cancellationToken);

        if (result.Any()) return result.FirstOrDefault();

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
        var page = pageRequest.GetPage();
        var pageSize = pageRequest.GetPageSize();

        return Context.WorkspaceAppUsers
            .Where(x => x.UserId == userId)
            .Select(w => w.Workspace)
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Workspace>> GetWorkspaces(CancellationToken cancellationToken = default, PageRequest? pageRequest = null)
    {
        pageRequest ??= new PageRequest();
        var page = pageRequest.GetPage();
        var pageSize = pageRequest.GetPageSize(PaginationDefaults.MaxAdminPageSize);

        return Entities
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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
}
