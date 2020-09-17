using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.ViewModels.Tags;

namespace Netptune.App.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TagsController : ControllerBase
    {
        private readonly ITagService TagService;

        public TagsController(ITagService tagService)
        {
            TagService = tagService;
        }

        // POST: api/tags/task
        [HttpPost]
        [Route("task")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(TagViewModel))]
        public async Task<IActionResult> PostTaskTag([FromBody] AddTagRequest request)
        {
            var result = await TagService.AddTagToTask(request);

            if (result is null) return NotFound();

            return Ok(result);
        }

        // GET: api/tags/taskId?workspace=workspaceSlug
        [Route("task/{systemId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(List<TagViewModel>))]
        public async Task<IActionResult> GetCommentsForTask([FromRoute] string systemId, [FromQuery] string workspace)
        {
            var result = await TagService.GetTagsForTask(systemId, workspace);

            if (result is null) return NotFound();

            return Ok(result);
        }
    }
}
