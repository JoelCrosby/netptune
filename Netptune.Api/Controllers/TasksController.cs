using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Services;
using Netptune.Models;
using Netptune.Models.Requests;
using Netptune.Models.ViewModels.ProjectTasks;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netptune.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectTasksController : ControllerBase
    {
        private readonly ITaskService TaskService;
        private readonly UserManager<AppUser> UserManager;

        public ProjectTasksController(ITaskService taskService, UserManager<AppUser> userManager)
        {
            TaskService = taskService;
            UserManager = userManager;
        }

        // GET: api/ProjectTasks
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/json", Type = typeof(List<TaskViewModel>))]
        public async Task<IActionResult> GetTasks(string workspaceSlug)
        {
            var result = await TaskService.GetTasks(workspaceSlug);

            return Ok(result);
        }

        // GET: api/ProjectTasks/5
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", Type = typeof(TaskViewModel))]
        public async Task<IActionResult> GetTask([FromRoute] int id)
        {
            var result = await TaskService.GetTask(id);

            return Ok(result);
        }

        // PUT: api/ProjectTasks
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(TaskViewModel))]
        public async Task<IActionResult> PutTask([FromBody] ProjectTask task)
        {
            var result = await TaskService.UpdateTask(task);

            return Ok(result);
        }

        // POST: api/ProjectTasks
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(TaskViewModel))]
        public async Task<IActionResult> PostTask([FromBody] AddProjectTaskRequest task)
        {
            var user = await UserManager.GetUserAsync(User);
            var result = await TaskService.AddTask(task, user);

            return Ok(result);
        }

        // DELETE: api/ProjectTasks/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", Type = typeof(TaskViewModel))]
        public async Task<IActionResult> DeleteTask([FromRoute] int id)
        {
            var user = await UserManager.GetUserAsync(User);
            var result = await TaskService.DeleteTask(id, user);

            return Ok(result);
        }

        // GET: api/ProjectTasks/GetProjectTaskCount/5
        [HttpGet]
        [Route("GetProjectTaskCount")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/json", Type = typeof(ProjectTaskCounts))]
        public async Task<IActionResult> GetProjectTaskCount(int projectId)
        {
            var result = await TaskService.GetProjectTaskCount(projectId);

            return Ok(result);
        }

        // POST: api/ProjectTasks/MoveTaskInGroup
        [HttpPost]
        [Route("MoveTaskInGroupRequest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(TaskViewModel))]
        public async Task<IActionResult> PostTask([FromBody] MoveTaskInGroupRequest request)
        {
            var user = await UserManager.GetUserAsync(User);
            var result = await TaskService.MoveTaskInBoardGroup(request, user);

            return Ok(result);
        }
    }
}