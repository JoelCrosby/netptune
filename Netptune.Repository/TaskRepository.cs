using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Netptune.Models.Entites;
using Netptune.Models.Enums;
using Netptune.Models.Models;
using Netptune.Models.Repositories;
using Netptune.Models.VeiwModels.ProjectTasks;
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

        public async Task<RepoResult<IEnumerable<TaskViewModel>>> GetTasksAsync(int workspaceId)
        {
            var tasks = _context.ProjectTasks
                .Where(x => x.Workspace.Id == workspaceId)
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
                    AssigneeUsername = r.Assignee.UserName,
                    AssigneePictureUrl = r.Assignee.UserName,
                    OwnerUsername = r.Owner.UserName,
                    ProjectName = r.Project.Name
                }).ToListAsync();

            if (result == null || !result.Any()) return RepoResult<IEnumerable<TaskViewModel>>.NotFound();

            return RepoResult<IEnumerable<TaskViewModel>>.Ok(result);
        }

        public async Task<RepoResult<ProjectTask>>GetTask(int id)
        {

            var task = await _context.ProjectTasks.FindAsync(id);

            if (task == null)
            {
                return RepoResult<ProjectTask>.NotFound();
            }

            return RepoResult<ProjectTask>.Ok(task);
        }

        public async Task<RepoResult<ProjectTask>> UpdateTask(ProjectTask projectTask)
        {
            if (projectTask == null)
            {
                return RepoResult<ProjectTask>.BadRequest();
            }

            var result = await _context.ProjectTasks.FirstOrDefaultAsync(x => x.Id == projectTask.Id);

            if (result == null) return RepoResult<ProjectTask>.NotFound();

            result.Name = projectTask.Name;
            result.Description = projectTask.Description;
            result.Status = projectTask.Status;
            result.SortOrder = projectTask.SortOrder;
            result.OwnerId = projectTask.OwnerId;
            result.AssigneeId = projectTask.AssigneeId;

            await _context.SaveChangesAsync();


            return RepoResult<ProjectTask>.Ok(result);
        }

        public async Task<RepoResult<ProjectTask>> AddTask(ProjectTask projectTask, AppUser user)
        {
            if (projectTask.ProjectId == null)
            {
                return RepoResult<ProjectTask>
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
                return RepoResult<ProjectTask>
                    .BadRequest("Could not find related project or workspace.");
            }

            projectTask.Workspace = relational.FirstOrDefault().workspace;
            projectTask.Project = relational.FirstOrDefault().project;
            projectTask.AssigneeId = user.Id;
            projectTask.OwnerId = user.Id;
            projectTask.CreatedByUserId = user.Id;

            var result = await _context.ProjectTasks.AddAsync(projectTask);
            await _context.SaveChangesAsync();


            return RepoResult<ProjectTask>.Ok(result.Entity);
        }

        public async Task<RepoResult<ProjectTask>> DeleteTask(int id)
        {
            var task = await _context.ProjectTasks.FindAsync(id);
            if (task == null)
            {
                return RepoResult<ProjectTask>.NotFound();
            }

            var result = _context.ProjectTasks.Remove(task);
            await _context.SaveChangesAsync();


            return RepoResult<ProjectTask>.Ok(result.Entity);
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