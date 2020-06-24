using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.App.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CommentController : ControllerBase
    {
        private readonly ICommentService CommentService;

        public CommentController(ICommentService commentService)
        {
            CommentService = commentService;
        }

        // POST: api/comment/task
        [HttpPost]
        [Route("task")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(TaskViewModel))]
        public async Task<IActionResult> PostTaskComment([FromBody] AddCommentRequest request)
        {
            var result = await CommentService.AddCommentToTask(request);

            if (result is null) return NotFound();

            return Ok(result);
        }

        // GET: api/comment/taskId?workspace=workspaceSlug
        [HttpGet("{systemId}")]
        [Route("task")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(TaskViewModel))]
        public async Task<IActionResult> GetCommentsForTask([FromRoute] string systemId, [FromQuery] string workspace)
        {
            var result = await CommentService.GetCommentsForTask(systemId, workspace);

            return Ok(result);
        }
    }
}
