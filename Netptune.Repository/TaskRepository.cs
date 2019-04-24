using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Netptune.Models.Entites;
using Netptune.Models.Enums;
using Netptune.Models.Models;
using Netptune.Models.Repositories;
using Netptune.Models.VeiwModels.ProjectTasks;
using Netptune.Repository.Permisions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Netptune.Repository
{
    public class TaskRepository : ITaskRepository
    {
        private readonly DataContext _context;
        private readonly UserManager<AppUser> _userManager;

        public TaskRepository(DataContext dataContext, UserManager<AppUser> userManager)
        {
            _context = dataContext;
            _userManager = userManager;
        }

        public async Task<RepoResult<TaskViewModel>> GetTaskAsync(int taskId)
        {
            var tasks = _context.ProjectTasks
                .Where(x => x.Id == taskId)
                .OrderBy(x => x.SortOrder)
                .Include(x => x.Assignee)
                .Include(x => x.Project)
                .Include(x => x.Owner);

            var result = await tasks
                .Select(task => new TaskViewModel()
                {
                    Id = task.Id,
                    AssigneeId = task.Assignee.Id,
                    Name = task.Name,
                    Description = task.Description,
                    Status = task.Status,
                    SortOrder = task.SortOrder,
                    ProjectId = task.ProjectId,
                    WorkspaceId = task.WorkspaceId,
                    CreatedAt = task.CreatedAt,
                    UpdatedAt = task.UpdatedAt,
                    AssigneeUsername = task.Assignee.GetDisplayName(),
                    AssigneePictureUrl = task.Assignee.GetDisplayName(),
                    OwnerUsername = task.Owner.GetDisplayName(),
                    ProjectName = task.Project.Name
                }).FirstOrDefaultAsync();

            if (result == null) return RepoResult<TaskViewModel>.NotFound();

            return RepoResult<TaskViewModel>.Ok(result);
        }

        public async Task<RepoResult<IEnumerable<TaskViewModel>>> GetTasksAsync(int workspaceId)
        {
            var tasks = _context.ProjectTasks
                .Where(x => x.Workspace.Id == workspaceId && !x.IsDeleted)
                .OrderBy(x => x.SortOrder)
                .Include(x => x.Assignee)
                .Include(x => x.Project)
                .Include(x => x.Owner);

            var result = await tasks
                .Select(r => new TaskViewModel()
                {
                    Id = r.Id,
                    AssigneeId = r.Assignee.Id,
                    Name = r.Name,
                    Description = r.Description,
                    Status = r.Status,
                    SortOrder = r.SortOrder,
                    ProjectId = r.ProjectId,
                    WorkspaceId = r.WorkspaceId,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    AssigneeUsername = r.Assignee.GetDisplayName(),
                    AssigneePictureUrl = r.Assignee.GetDisplayName(),
                    OwnerUsername = r.Owner.GetDisplayName(),
                    ProjectName = r.Project.Name
                }).ToListAsync();

            if (result == null) return RepoResult<IEnumerable<TaskViewModel>>.NotFound();

            return RepoResult<IEnumerable<TaskViewModel>>.Ok(result);
        }

        public async Task<RepoResult<ProjectTask>> GetTask(int id)
        {

            var task = await _context.ProjectTasks.FindAsync(id);

            if (task == null)
            {
                return RepoResult<ProjectTask>.NotFound();
            }

            return RepoResult<ProjectTask>.Ok(task);
        }

        public async Task<RepoResult<TaskViewModel>> UpdateTask(ProjectTask projectTask, AppUser user)
        {
            if (projectTask == null)
            {
                return RepoResult<TaskViewModel>.BadRequest();
            }

            var result = await _context.ProjectTasks.FirstOrDefaultAsync(x => x.Id == projectTask.Id);

            if (result == null) return RepoResult<TaskViewModel>.NotFound();

            if (!TaskPermisions.CanEdit(result, user))
                return RepoResult<TaskViewModel>.Unauthorized();

            result.Name = projectTask.Name;
            result.Description = projectTask.Description;
            result.Status = projectTask.Status;
            result.SortOrder = projectTask.SortOrder;
            result.OwnerId = projectTask.OwnerId;
            result.AssigneeId = projectTask.AssigneeId;

            await _context.SaveChangesAsync();

            var response = await GetTaskAsync(result.Id);

            return RepoResult<TaskViewModel>.Ok(response.Result);
        }

        public async Task<RepoResult<TaskViewModel>> AddTask(ProjectTask projectTask, AppUser user)
        {
            if (projectTask.ProjectId == null)
            {
                return RepoResult<TaskViewModel>
                    .BadRequest("Could not determine the project for task.");
            }

            // Load the relationship tables.
            _context.ProjectTasks.Include(m => m.Workspace).ThenInclude(e => e.Projects);

            var relational = (from w in _context.Workspaces
                              join p in _context.Projects
                              on projectTask.ProjectId equals p.Id
                              where
                              w.Id == projectTask.WorkspaceId
                              select new
                              {
                                  project = p,
                                  workspace = w
                              }).Take(1);

            if (!relational.Any())
            {
                return RepoResult<TaskViewModel>
                    .BadRequest("Could not find related project or workspace.");
            }

            projectTask.Workspace = relational.FirstOrDefault().workspace;
            projectTask.Project = relational.FirstOrDefault().project;
            projectTask.AssigneeId = user.Id;
            projectTask.OwnerId = user.Id;
            projectTask.CreatedByUserId = user.Id;

            var result = await _context.ProjectTasks.AddAsync(projectTask);
            await _context.SaveChangesAsync();

            var response = await GetTaskAsync(result.Entity.Id);

            return RepoResult<TaskViewModel>.Ok(response.Result);
        }

        public async Task<RepoResult<int>> DeleteTask(int id, AppUser user)
        {
            var task = await _context.ProjectTasks.FindAsync(id);
            if (task == null)
            {
                return RepoResult<int>.NotFound();
            }

            task.IsDeleted = true;
            task.DeletedByUserId = user.Id;

            await _context.SaveChangesAsync();

            return RepoResult<int>.Ok(id);
        }

        public async Task<RepoResult<ProjectTaskCounts>> GetProjectTaskCount(int projectId)
        {
            var tasks = _context.ProjectTasks.Where(x => x.ProjectId == projectId && !x.IsDeleted);

            var result =
                await Task.FromResult(new ProjectTaskCounts()
                {
                    AllTasks = tasks.Count(),
                    CompletedTasks = tasks.Count(x => x.Status == ProjectTaskStatus.Complete),
                    InProgressTasks = tasks.Count(x => x.Status == ProjectTaskStatus.InProgress),
                    BacklogTasks = tasks.Count(x => x.Status == ProjectTaskStatus.UnAssigned)
                });


            return RepoResult<ProjectTaskCounts>.Ok(result);
        }
    }
}