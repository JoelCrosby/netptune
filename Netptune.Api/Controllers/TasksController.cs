using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using Netptune.Api.Extensions;
using Netptune.Core.Services;
using Netptune.Models;
using Netptune.Models.VeiwModels.ProjectTasks;

namespace Netptune.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectTasksController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly UserManager<AppUser> _userManager;

        public ProjectTasksController(ITaskService taskService, UserManager<AppUser> userManager)
        {
            _taskService = taskService;
            _userManager = userManager;
        }

        // GET: api/ProjectTasks
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/json", Type = typeof(List<TaskViewModel>))]
        public async Task<IActionResult> GetTasks(int workspaceId)
        {
            var result = await _taskService.GetTasks(workspaceId);

            return result.ToRestResult();
        }

        // GET: api/ProjectTasks/5
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", Type = typeof(TaskViewModel))]
        public async Task<IActionResult> GetTask([FromRoute] int id)
        {
            var result = await _taskService.GetTask(id);

            return result.ToRestResult();
        }

        // PUT: api/ProjectTasks/5
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(TaskViewModel))]
        public async Task<IActionResult> PutTask([FromRoute] int id, [FromBody] ProjectTask task)
        {
            var user = await _userManager.GetUserAsync(User);
            var result = await _taskService.UpdateTask(task, user);

            return result.ToRestResult();
        }

        // POST: api/ProjectTasks
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(TaskViewModel))]
        public async Task<IActionResult> PostTask([FromBody] ProjectTask task)
        {
            var user = await _userManager.GetUserAsync(User);
            var result = await _taskService.AddTask(task, user);

            return result.ToRestResult();
        }

        // DELETE: api/ProjectTasks/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", Type = typeof(TaskViewModel))]
        public async Task<IActionResult> DeleteTask([FromRoute] int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var result = await _taskService.DeleteTask(id, user);

            return result.ToRestResult();
        }

        // GET: api/ProjectTasks/GetProjectTaskCount/5
        [HttpGet]
        [Route("GetProjectTaskCount")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/json", Type = typeof(ProjectTaskCounts))]
        public async Task<IActionResult> GetProjectTaskCount(int projectId)
        {
            var result = await _taskService.GetProjectTaskCount(projectId);

            return result.ToRestResult();
        }
    }
}