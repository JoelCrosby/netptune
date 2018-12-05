using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netptune.Models.Entites;
using Netptune.Models.Enums;
using Netptune.Models.Models;
using Netptune.Models.Models.ViewModels;

namespace Netptune.Api.Controllers
{

    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectTasksController : ControllerBase
    {

        private readonly ProjectsContext _context;
        private readonly UserManager<AppUser> _userManager;

        public ProjectTasksController(ProjectsContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public class TaskDto
        {
            public int Id { get; set; }
            public string AssigneeId { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public ProjectTaskStatus Status { get; set; }
            public double SortOrder { get; set; }
            public int? ProjectId { get; set; }
            public int? WorkspaceId { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset UpdatedAt { get; set; }
            public string AssigneeUsername { get; set; }
            public string AssigneePictureUrl { get; set; }
            public string OwnerUsername { get; set; }
            public string ProjectName { get; set; }
        }

        // GET: api/Tasks
        [HttpGet]
        public IEnumerable<TaskDto> GetTasks(int workspaceId)
        {

            var result = _context.ProjectTasks
                .Where(x => x.Workspace.Id == workspaceId)
                .OrderBy(x => x.SortOrder)
                .Include(x => x.Assignee)
                .Include(x => x.Project)
                .Include(x => x.Owner);

            return result
                .Select(r => new TaskDto() {
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
                }).ToList();

        }

        // GET: api/Tasks/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTask([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var task = await _context.ProjectTasks.FindAsync(id);

            if (task == null)
            {
                return NotFound();
            }

            return Ok(task);
        }

        // PUT: api/Tasks/5
        [HttpPut("{id}")]
        public IActionResult PutTask([FromRoute] int id, [FromBody] ProjectTask task)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != task.Id)
            {
                return BadRequest();
            }

            var fromDb = _context.ProjectTasks.FirstOrDefault(x => x.Id == task.Id);

            fromDb.Name = task.Name;
            fromDb.Description = task.Description;
            fromDb.Status = task.Status;
            fromDb.SortOrder = task.SortOrder;
            fromDb.OwnerId = task.OwnerId;
            fromDb.AssigneeId = task.AssigneeId;

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaskExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok(task);
        }

        // POST: api/Tasks
        [HttpPost]
        public async Task<IActionResult> PostTask([FromBody] ProjectTask task)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (task.ProjectId == null) 
            {
                return BadRequest("Could not determine the project for task.");
            }

            // Load the relationship tables.
            _context.ProjectTasks.Include(m => m.Workspace).ThenInclude(e => e.Projects);

            var relational = (from w in _context.Workspaces
                                join p in _context.Projects 
                                on task.ProjectId equals p.Id
                                where
                                w.Id == task.WorkspaceId
                                select new {
                                    project = p,
                                    workspace = w
                                }).Take(1);

            if (!relational.Any())
            {
                return BadRequest("Could not find related project or workspace!");
            }

            task.Workspace = relational.FirstOrDefault().workspace;
            task.Project = relational.FirstOrDefault().project;

            var user = await _userManager.GetUserAsync(User) as AppUser;
            task.Assignee = user;
            task.Owner = user;
            task.CreatedByUser = user;

            _context.ProjectTasks.Add(task);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTask", new { id = task.Id }, task);
        }

        // DELETE: api/Tasks/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var task = await _context.ProjectTasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            _context.ProjectTasks.Remove(task);
            await _context.SaveChangesAsync();

            return Ok(task);
        }

        private bool TaskExists(int id)
        {
            return _context.ProjectTasks.Any(e => e.Id == id);
        }

        [HttpPost]
        [Route("UpdateSortOrder")]
        public async Task<IActionResult> UpdateSortOrder(ProjectTask[] tasks)
        {
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i].SortOrder = i;
                _context.Entry(tasks[i]).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();
            return Ok(tasks);

        }

        [HttpGet]
        [Route("GetProjectTaskCount")]
        public async Task<IActionResult> GetProjectTaskCount(int projectId)
        {
            var tasks = _context.ProjectTasks.Where(x => x.ProjectId == projectId && !x.IsDeleted);

            return Ok(
                await Task.FromResult(new ProjectTaskCounts() 
                { 
                    AllTasks  = tasks.Count(),
                    CompletedTasks = tasks.Count(x => x.Status == ProjectTaskStatus.Complete),
                    InProgressTasks = tasks.Count(x => x.Status == ProjectTaskStatus.InProgress),
                    BacklogTasks = tasks.Count(x => x.Status == ProjectTaskStatus.UnAssigned)
                })
            );
        }
    }
}