using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Services;

namespace Netptune.App.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly ICommentService CommentService;

        public CommentsController(ICommentService commentService)
        {
            CommentService = commentService;
        }

        // POST: api/comment/task
        [HttpPost]
        [Route("task")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(Comment))]
        public async Task<IActionResult> PostTaskComment([FromBody] AddCommentRequest request)
        {
            var result = await CommentService.AddCommentToTask(request);

            if (result is null) return NotFound();

            return Ok(result);
        }

        // GET: api/comments/taskId?workspace=workspaceSlug
        [Route("task/{systemId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(List<Comment>))]
        public async Task<IActionResult> GetCommentsForTask([FromRoute] string systemId, [FromQuery] string workspace)
        {
            var result = await CommentService.GetCommentsForTask(systemId, workspace);

            if (result is null) return NotFound();

            return Ok(result);
        }
    }
}
