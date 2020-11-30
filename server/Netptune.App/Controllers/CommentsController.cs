using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.ViewModels.Comments;

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
        [Produces("application/json", Type = typeof(CommentViewModel))]
        public async Task<IActionResult> PostTaskComment([FromBody] AddCommentRequest request)
        {
            var result = await CommentService.AddCommentToTask(request);

            if (result is null) return NotFound();

            return Ok(result);
        }

        // GET: api/comments/taskId
        [Route("task/{systemId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(List<CommentViewModel>))]
        public async Task<IActionResult> GetCommentsForTask([FromRoute] string systemId)
        {
            var result = await CommentService.GetCommentsForTask(systemId);

            if (result is null) return NotFound();

            return Ok(result);
        }

        // DELETE: api/comments/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", Type = typeof(CommentViewModel))]
        public async Task<IActionResult> DeleteBoard([FromRoute] int id)
        {
            var result = await CommentService.Delete(id);

            if (result is null) return NotFound();

            return Ok(result);
        }
    }
}
