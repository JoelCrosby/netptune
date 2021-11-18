using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.ViewModels.Tags;

namespace Netptune.App.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = NetptunePolicies.Workspace)]
public class TagsController : ControllerBase
{
    private readonly ITagService TagService;

    public TagsController(ITagService tagService)
    {
        TagService = tagService;
    }

    // POST: api/tags
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json", Type = typeof(TagViewModel))]
    public async Task<IActionResult> PostTag([FromBody] AddTagRequest request)
    {
        var result = await TagService.AddTag(request);

        if (result is null) return NotFound();

        return Ok(result);
    }

    // POST: api/tags/task
    [HttpPost]
    [Route("task")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json", Type = typeof(TagViewModel))]
    public async Task<IActionResult> PostTaskTag([FromBody] AddTagToTaskRequest request)
    {
        var result = await TagService.AddTagToTask(request);

        if (result is null) return NotFound();

        return Ok(result);
    }

    // GET: api/tags/taskId
    [Route("task/{systemId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json", Type = typeof(List<TagViewModel>))]
    public async Task<IActionResult> GetTagsForTask([FromRoute] string systemId)
    {
        var result = await TagService.GetTagsForTask(systemId);

        if (result is null) return NotFound();

        return Ok(result);
    }

    // GET: api/tags/workspace
    [Route("workspace")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json", Type = typeof(List<TagViewModel>))]
    public async Task<IActionResult> GetTagsForTask()
    {
        var result = await TagService.GetTagsForWorkspace();

        if (result is null) return NotFound();

        return Ok(result);
    }

    // DELETE: api/tags/
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Produces("application/json", Type = typeof(ClientResponse))]
    public async Task<IActionResult> Delete([FromBody] DeleteTagsRequest request)
    {
        var result = await TagService.Delete(request);

        return Ok(result);
    }

    // DELETE: api/tags/task
    [HttpDelete]
    [Route("task")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json", Type = typeof(ClientResponse))]
    public async Task<IActionResult> DeleteFromTask([FromBody] DeleteTagFromTaskRequest request)
    {
        var result = await TagService.DeleteFromTask(request);

        if (result is null) return NotFound();

        return Ok(result);
    }

    // PATCH: api/tags
    [HttpPatch]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json", Type = typeof(ClientResponse))]
    public async Task<IActionResult> UpdateTag([FromBody] UpdateTagRequest request)
    {
        var result = await TagService.Update(request);

        if (result is null) return NotFound();

        return Ok(result);
    }
}