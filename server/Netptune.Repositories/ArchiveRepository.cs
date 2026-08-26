using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Relationships;
using Netptune.Entities.Contexts;
using Netptune.Transfer.Repositories;

namespace Netptune.Repositories;

public sealed class ArchiveRepository(DataContext context) : IArchiveRepository
{
    public Task<Workspace?> GetWorkspace(int workspaceId, CancellationToken cancellationToken = default)
    {
        return context.Workspaces
            .AsNoTracking()
            .SingleOrDefaultAsync(workspace => workspace.Id == workspaceId, cancellationToken);
    }

    public IAsyncEnumerable<WorkspaceAppUser> ReadMembers(int workspaceId, CancellationToken cancellationToken = default)
    {
        return context.WorkspaceAppUsers
            .AsNoTracking()
            .Include(member => member.User)
            .Where(member => member.WorkspaceId == workspaceId)
            .OrderBy(member => member.Id)
            .AsAsyncEnumerable();
    }

    public IAsyncEnumerable<Status> ReadStatuses(int workspaceId, CancellationToken cancellationToken = default)
    {
        return Live(context.Statuses, workspaceId).OrderBy(status => status.Id).AsAsyncEnumerable();
    }

    public IAsyncEnumerable<Tag> ReadTags(int workspaceId, CancellationToken cancellationToken = default)
    {
        return Live(context.Tags, workspaceId).OrderBy(tag => tag.Id).AsAsyncEnumerable();
    }

    public IAsyncEnumerable<RelationType> ReadRelationTypes(int workspaceId, CancellationToken cancellationToken = default)
    {
        return Live(context.RelationTypes, workspaceId).OrderBy(relationType => relationType.Id).AsAsyncEnumerable();
    }

    public IAsyncEnumerable<Project> ReadProjects(int workspaceId, CancellationToken cancellationToken = default)
    {
        return Live(context.Projects, workspaceId)
            .Include(project => project.DefaultStatus)
            .Include(project => project.ProjectUsers)
            .ThenInclude(link => link.User)
            .OrderBy(project => project.Id)
            .AsAsyncEnumerable();
    }

    public IAsyncEnumerable<Board> ReadBoards(int workspaceId, CancellationToken cancellationToken = default)
    {
        return Live(context.Boards, workspaceId)
            .Where(board => board.Project != null && !board.Project.IsDeleted)
            .Include(board => board.Project)
            .OrderBy(board => board.Id)
            .AsAsyncEnumerable();
    }

    public IAsyncEnumerable<BoardGroup> ReadBoardGroups(int workspaceId, CancellationToken cancellationToken = default)
    {
        return Live(context.BoardGroups, workspaceId)
            .Where(group => group.Board != null && !group.Board.IsDeleted)
            .Where(group => group.Board!.Project != null && !group.Board.Project.IsDeleted)
            .Include(group => group.Board)
            .Include(group => group.Status)
            .OrderBy(group => group.Id)
            .AsAsyncEnumerable();
    }

    public IAsyncEnumerable<Sprint> ReadSprints(int workspaceId, CancellationToken cancellationToken = default)
    {
        return Live(context.Sprints, workspaceId)
            .Where(sprint => sprint.Project != null && !sprint.Project.IsDeleted)
            .Include(sprint => sprint.Project)
            .OrderBy(sprint => sprint.Id)
            .AsAsyncEnumerable();
    }

    public IAsyncEnumerable<ProjectTask> ReadTasks(int workspaceId, CancellationToken cancellationToken = default)
    {
        return Live(context.ProjectTasks, workspaceId)
            .Where(task => task.Project != null && !task.Project.IsDeleted)
            .Include(task => task.Project)
            .Include(task => task.Status)
            .Include(task => task.Sprint).ThenInclude(sprint => sprint!.Project)
            .Include(task => task.CreatedByUser)
            .OrderBy(task => task.Id)
            .AsAsyncEnumerable();
    }

    public IAsyncEnumerable<ProjectTaskAppUser> ReadTaskAssignees(int workspaceId, CancellationToken cancellationToken = default)
    {
        return context.ProjectTaskAppUsers
            .AsNoTracking()
            .Include(link => link.ProjectTask).ThenInclude(task => task.Project)
            .Include(link => link.User)
            .Where(link => link.ProjectTask.WorkspaceId == workspaceId && !link.ProjectTask.IsDeleted)
            .OrderBy(link => link.Id)
            .AsAsyncEnumerable();
    }

    public IAsyncEnumerable<ProjectTaskTag> ReadTaskTags(int workspaceId, CancellationToken cancellationToken = default)
    {
        return context.ProjectTaskTags
            .AsNoTracking()
            .Include(link => link.ProjectTask).ThenInclude(task => task!.Project)
            .Include(link => link.Tag)
            .Where(link => link.ProjectTask!.WorkspaceId == workspaceId && !link.ProjectTask.IsDeleted)
            .Where(link => !link.Tag!.IsDeleted)
            .OrderBy(link => link.Id)
            .AsAsyncEnumerable();
    }

    public IAsyncEnumerable<ProjectTaskInBoardGroup> ReadTaskPlacements(int workspaceId, CancellationToken cancellationToken = default)
    {
        return context.ProjectTaskInBoardGroups
            .AsNoTracking()
            .Include(link => link.ProjectTask).ThenInclude(task => task!.Project)
            .Include(link => link.BoardGroup).ThenInclude(group => group!.Board)
            .Where(link => link.ProjectTask!.WorkspaceId == workspaceId && !link.ProjectTask.IsDeleted)
            .OrderBy(link => link.Id)
            .AsAsyncEnumerable();
    }

    public IAsyncEnumerable<Reaction> ReadReactions(int workspaceId, CancellationToken cancellationToken = default)
    {
        return Live(context.Reactions, workspaceId)
            .Include(reaction => reaction.CreatedByUser)
            .OrderBy(reaction => reaction.Id)
            .AsAsyncEnumerable();
    }

    public IAsyncEnumerable<ProjectTaskRelation> ReadTaskRelations(int workspaceId, CancellationToken cancellationToken = default)
    {
        return context.ProjectTaskRelations
            .AsNoTracking()
            .Include(relation => relation.RelationType)
            .Include(relation => relation.SourceTask).ThenInclude(task => task!.Project)
            .Include(relation => relation.TargetTask).ThenInclude(task => task!.Project)
            .Where(relation => relation.WorkspaceId == workspaceId)
            .OrderBy(relation => relation.Id)
            .AsAsyncEnumerable();
    }

    public IAsyncEnumerable<Comment> ReadComments(int workspaceId, CancellationToken cancellationToken = default)
    {
        return Live(context.Comments, workspaceId)
            .Include(comment => comment.CreatedByUser)
            .OrderBy(comment => comment.Id)
            .AsAsyncEnumerable();
    }

    public IAsyncEnumerable<Flag> ReadFlags(int workspaceId, CancellationToken cancellationToken = default)
    {
        return Live(context.Flags, workspaceId).OrderBy(flag => flag.Id).AsAsyncEnumerable();
    }

    public IAsyncEnumerable<AutomationRule> ReadAutomations(int workspaceId, CancellationToken cancellationToken = default)
    {
        return Live(context.AutomationRules, workspaceId)
            .Include(rule => rule.Actions)
            .OrderBy(rule => rule.Id)
            .AsAsyncEnumerable();
    }

    public IAsyncEnumerable<WorkspaceFile> ReadFiles(int workspaceId, CancellationToken cancellationToken = default)
    {
        return Live(context.WorkspaceFiles, workspaceId)
            .Where(file => file.Status == WorkspaceFileStatus.Ready && !file.QuotaReleased)
            .OrderBy(file => file.Id)
            .AsAsyncEnumerable();
    }

    public IAsyncEnumerable<EventRecord> ReadEvents(int workspaceId, CancellationToken cancellationToken = default)
    {
        return context.EventRecords
            .AsNoTracking()
            .Where(record => record.WorkspaceId == workspaceId)
            .OrderBy(record => record.Id)
            .AsAsyncEnumerable();
    }

    public Task<long> GetFileBytes(int workspaceId, CancellationToken cancellationToken = default)
    {
        return context.WorkspaceFiles
            .AsNoTracking()
            .Where(file => file.WorkspaceId == workspaceId && !file.IsDeleted && !file.QuotaReleased)
            .Where(file => file.Status == WorkspaceFileStatus.Ready)
            .SumAsync(file => file.SizeBytes, cancellationToken);
    }

    private static IQueryable<TEntity> Live<TEntity>(DbSet<TEntity> entities, int workspaceId)
        where TEntity : Core.BaseEntities.WorkspaceEntity<int>
    {
        return entities
            .AsNoTracking()
            .Where(entity => entity.WorkspaceId == workspaceId && !entity.IsDeleted);
    }
}
