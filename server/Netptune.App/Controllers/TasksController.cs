using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService TaskService;

        public TasksController(ITaskService taskService)
        {
            TaskService = taskService;
        }

        // GET: api/Tasks?workspaceSlug=workspace
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/json", Type = typeof(List<TaskViewModel>))]
        public async Task<IActionResult> GetTasks([FromQuery] string workspaceSlug)
        {
            var result = await TaskService.GetTasks(workspaceSlug);

            return Ok(result);
        }

        // GET: api/Tasks/5
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", Type = typeof(TaskViewModel))]
        public async Task<IActionResult> GetTask([FromRoute] int id)
        {
            var result = await TaskService.GetTask(id);

            if (result is null) return NotFound();

            return Ok(result);
        }

        // GET: api/Tasks/detail?systemId=proj-5&workspace=workspace
        [HttpGet]
        [Route("detail")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", Type = typeof(TaskViewModel))]
        public async Task<IActionResult> GetTask([FromQuery] string systemId, [FromQuery] string workspace)
        {
            var result = await TaskService.GetTaskDetail(systemId, workspace);

            if (result is null) return NotFound();

            return Ok(result);
        }

        // PUT: api/Tasks
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(TaskViewModel))]
        public async Task<IActionResult> PutTask([FromBody] UpdateProjectTaskRequest request)
        {
            var result = await TaskService.Update(request);

            if (result is null) return NotFound();

            return Ok(result);
        }

        // POST: api/Tasks
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(TaskViewModel))]
        public async Task<IActionResult> PostTask([FromBody] AddProjectTaskRequest request)
        {
            var result = await TaskService.Create(request);

            return Ok(result);
        }

        // DELETE: api/Tasks
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", Type = typeof(ClientResponse))]
        public async Task<IActionResult> DeleteTask([FromBody] IEnumerable<int> ids)
        {
            var result = await TaskService.Delete(ids);

            if (result is null) return NotFound();

            return Ok(result);
        }

        // DELETE: api/Tasks/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", Type = typeof(ClientResponse))]
        public async Task<IActionResult> DeleteTask([FromRoute] int id)
        {
            var result = await TaskService.Delete(id);

            if (result is null) return NotFound();

            return Ok(result);
        }

        // GET: api/Tasks/count/5
        [HttpGet]
        [Route("count")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/json", Type = typeof(ProjectTaskCounts))]
        public async Task<IActionResult> GetProjectTaskCount(int projectId)
        {
            var result = await TaskService.GetProjectTaskCount(projectId);

            return Ok(result);
        }

        // POST: api/Tasks/move-task-in-group
        [HttpPost]
        [Route("move-task-in-group")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(TaskViewModel))]
        public async Task<IActionResult> MoveTaskInGroup([FromBody] MoveTaskInGroupRequest request)
        {
            var result = await TaskService.MoveTaskInBoardGroup(request);

            if (result is null) return NotFound();

            return Ok(result);
        }

        // POST: api/Tasks/move-tasks-to-group
        [HttpPost]
        [Route("move-tasks-to-group")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(ClientResponse))]
        public async Task<IActionResult> MoveTasksToGroup([FromBody] MoveTasksToGroupRequest request)
        {
            var result = await TaskService.MoveTasksToGroup(request);

            if (result is null) return NotFound();

            return Ok(result);
        }

        // GET: api/tasks/export-workspace
        [HttpGet]
        [Route("export-workspace/{workspace}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(TaskViewModel))]
        public async Task<IActionResult> ExportWorkspaceTasks([FromRoute] string workspace)
        {
            var result = await TaskService.ExportWorkspaceTasks(workspace);

            return File(result.Stream, result.ContentType, result.Filename);
        }
    }
}
