using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataPlane.Entites;
using DataPlane.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using DataPlane.Interfaces;
using System;

namespace DataPlane.Controllers
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

        // GET: api/Tasks
        [HttpGet]
        public IEnumerable<ProjectTask> GetTasks(int workspaceId)
        {
            return _context.ProjectTasks.Where(x => x.Workspace.WorkspaceId == workspaceId).OrderBy(x => x.SortOrder)
                .Include(x => x.Assignee).Include(x => x.Project).Include(x => x.Owner);
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

            if (id != task.ProjectTaskId)
            {
                return BadRequest();
            }

            var fromDb = _context.ProjectTasks.FirstOrDefault(x => x.ProjectTaskId == task.ProjectTaskId);

            fromDb.Name = task.Name;
            fromDb.Description = task.Description;
            fromDb.Status = task.Status;
            fromDb.SortOrder = task.SortOrder;
            fromDb.OwnerId = task.OwnerId;
            fromDb.AssigneeId = task.AssigneeId;

            // _context.Entry(task).State = EntityState.Modified;

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

            if (task.ProjectId == null || task.Project == null) 
            {
                return BadRequest("Could not deter,mine the project for task.");
            }

            // Load the relationship tables.
            _context.ProjectTasks.Include(m => m.Workspace).ThenInclude(e => e.Projects);

            var relational = (from w in _context.Workspaces
                                join p in _context.Projects 
                                on new { ProjectId = task.ProjectId ?? task.Project.ProjectId } 
                                equals new { ProjectId = p.ProjectId }
                                where
                                w.WorkspaceId == (task.WorkspaceId ?? task.Workspace.WorkspaceId)
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

            return CreatedAtAction("GetTask", new { id = task.ProjectTaskId }, task);
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
            return _context.ProjectTasks.Any(e => e.ProjectTaskId == id);
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

            try
            {
                await _context.SaveChangesAsync();
                return Ok(tasks);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}