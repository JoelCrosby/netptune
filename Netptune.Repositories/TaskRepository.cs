using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories
{
    public class TaskRepository : Repository<DataContext, ProjectTask, int>, ITaskRepository
    {
        public TaskRepository(DataContext context, IDbConnectionFactory connectionFactory)
            : base(context, connectionFactory)
        {
        }

        public Task<TaskViewModel> GetTaskViewModel(int taskId)
        {
            return Entities
                .Where(x => x.Id == taskId)
                .OrderBy(x => x.SortOrder)
                .Include(x => x.Assignee)
                .Include(x => x.Project)
                .Include(x => x.Owner)
                .Include(x => x.Workspace)
                .AsNoTracking()
                .Select(task => task.ToViewModel())
                .FirstOrDefaultAsync();
        }

        public async Task<int?> GetTaskInternalId(string systemId, string workspaceSlug)
        {
            var entity = await GetTaskFromSystemId(systemId, workspaceSlug, true);

            var task = await entity.FirstOrDefaultAsync();

            return task?.Id;
        }

        public async Task<ProjectTask> GetTask(string systemId, string workspaceSlug)
        {
            var entity = await GetTaskFromSystemId(systemId, workspaceSlug, true);

            return await entity.FirstOrDefaultAsync();
        }

        public async Task<TaskViewModel> GetTaskViewModel(string systemId, string workspaceSlug)
        {
            var entity = await GetTaskFromSystemId(systemId, workspaceSlug, true);

            return await entity
                .Select(task => task.ToViewModel())
                .FirstOrDefaultAsync();
        }

        private async Task<IQueryable<ProjectTask>> GetTaskFromSystemId(string systemId, string workspaceSlug, bool isReadonly = false)
        {
            var parts = systemId.Split("-");

            var hasProjectId = int.TryParse(parts.LastOrDefault(), out var projectScopeId);

            if (!hasProjectId) return null;

            var projectKey = parts.First();

            var workspace = await Context.Workspaces
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Slug == workspaceSlug);

            if (workspace is null) return null;

            var project = await Context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Key == projectKey && x.WorkspaceId == workspace.Id);

            if (project is null) return null;

            var queryable = Entities
                .Where(x => x.ProjectScopeId == projectScopeId && x.WorkspaceId == workspace.Id && x.ProjectId == project.Id)
                .OrderBy(x => x.SortOrder)
                .Include(x => x.Assignee)
                .Include(x => x.Project)
                .Include(x => x.Owner)
                .Include(x => x.Workspace);

            return isReadonly ? queryable.AsNoTracking() : queryable;
        }

        public Task<List<TaskViewModel>> GetTasksAsync(string workspaceSlug, bool isReadonly = false)
        {
            return Entities
                .Where(x => x.Workspace.Slug == workspaceSlug && !x.IsDeleted)
                .OrderBy(x => x.SortOrder)
                .Include(x => x.Assignee)
                .Include(x => x.Project)
                .Include(x => x.Owner)
                .Include(x => x.Workspace)
                .Select(task => task.ToViewModel())
                .ApplyReadonly(isReadonly);
        }

        public Task<List<ExportTaskViewModel>> GetExportTasksAsync(string workspaceSlug)
        {
            return (from t in Entities
                join tg in Context.ProjectTaskInBoardGroups on t.Id equals tg.ProjectTaskId
                join g in Context.BoardGroups on tg.BoardGroupId equals g.Id
                join project in Context.Projects on t.ProjectId equals project.Id
                join assignee in Context.AppUsers on t.AssigneeId equals assignee.Id
                join owner in Context.AppUsers on t.OwnerId equals owner.Id
                join workspace in Context.Workspaces on t.WorkspaceId equals workspace.Id
                where !workspace.IsDeleted && !t.IsDeleted && workspace.Slug == workspaceSlug
                orderby t.SortOrder
                select new
                {
                    t.Id,
                    AssigneeId = assignee.Id,
                    OwnerId = owner.Id,
                    t.ProjectScopeId,
                    t.Name,
                    project.Key,
                    t.Description,
                    t.Status,
                    t.IsFlagged,
                    t.SortOrder,
                    t.ProjectId,
                    t.WorkspaceId,
                    WorkspaceSlug = workspace.Slug,
                    t.CreatedAt,
                    t.UpdatedAt,
                    AssigneeUsername = assignee.UserName,
                    AssigneePictureUrl = assignee.PictureUrl,
                    OwnerUsername = owner.UserName,
                    OwnerPictureUrl = owner.PictureUrl,
                    ProjectName = project.Name,
                    Group = g.Name,
                }).Select(x => new ExportTaskViewModel
            {
                Id = x.Id,
                AssigneeId = x.AssigneeId,
                OwnerId = x.OwnerId,
                ProjectScopeId = x.ProjectScopeId,
                Name = x.Name,
                Description = x.Description,
                SystemId = $"{x.Key}-{x.ProjectScopeId}",
                Status = x.Status.ToString(),
                IsFlagged = x.IsFlagged,
                SortOrder = x.SortOrder,
                ProjectId = x.ProjectId,
                WorkspaceId = x.WorkspaceId,
                WorkspaceSlug = x.WorkspaceSlug,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                AssigneeUsername = x.AssigneeUsername,
                AssigneePictureUrl = x.AssigneePictureUrl,
                OwnerUsername = x.OwnerUsername,
                OwnerPictureUrl = x.OwnerPictureUrl,
                ProjectName = x.ProjectName,
                Group = x.Group,
            })
                .ToListAsync();
        }

        public async Task<ProjectTaskCounts> GetProjectTaskCount(int projectId)
        {
            var tasks = await Entities
                .Where(x => x.ProjectId == projectId && !x.IsDeleted)
                .AsNoTracking()
                .ToListAsync();

            return new ProjectTaskCounts
            {
                AllTasks = tasks.Count,
                CompletedTasks = tasks.Count(x => x.Status == ProjectTaskStatus.Complete),
                InProgressTasks = tasks.Count(x => x.Status == ProjectTaskStatus.InProgress),
                BacklogTasks = tasks.Count(x => x.Status == ProjectTaskStatus.UnAssigned)
            };
        }

        public async Task<int?> GetNextScopeId(int projectId, int increment = 0)
        {
            var taskCount = await Entities.CountAsync(x => x.ProjectId == projectId);

            return taskCount + 1 + increment;
        }
    }
}
