using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.App.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = NetptunePolicies.Workspace)]
public class BoardsController : ControllerBase
{
    private readonly IBoardService BoardService;

    public BoardsController(IBoardService boardService)
    {
        BoardService = boardService;
    }

    // GET: api/boards/5
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json", Type = typeof(Board))]
    public async Task<IActionResult> GetBoard([FromRoute] int id)
    {
        var result = await BoardService.GetBoard(id);

        if (result.IsNotFound) return NotFound();

        return Ok(result);
    }

    // GET: api/boards/workspace
    [HttpGet("workspace")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json", Type = typeof(List<BoardViewModel>))]
    public async Task<IActionResult> GetBoardsInWorkspace()
    {
        var result = await BoardService.GetBoardsInWorkspace();

        if (result is null) return NotFound();

        return Ok(result);
    }

    // GET: api/boards/project/{id}
    [HttpGet("project/{projectId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json", Type = typeof(List<BoardViewModel>))]
    public async Task<IActionResult> GetBoardsInProject([Required] int? projectId)
    {
        if (!projectId.HasValue) return BadRequest();

        var result = await BoardService.GetBoardsInProject(projectId.Value);

        if (result is null) return NotFound();

        return Ok(result);
    }

    // GET: api/boards/view/identifier
    [HttpGet("view/{identifier}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json", Type = typeof(BoardView))]
    public async Task<IActionResult> GetBoardView(string identifier, [FromQuery] BoardGroupsFilter filter)
    {
        var result = await BoardService.GetBoardView(identifier, filter);

        if (result.IsNotFound) return NotFound();

        return Ok(result);
    }

    // PUT: api/boards/5
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json", Type = typeof(ClientResponse<BoardViewModel>))]
    public async Task<IActionResult> PutBoard([FromBody] UpdateBoardRequest request)
    {
        var result = await BoardService.Update(request);

        if (result.IsNotFound) return NotFound();

        return Ok(result);
    }

    // POST: api/boards
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json", Type = typeof(ClientResponse<BoardViewModel>))]
    public async Task<IActionResult> PostBoard([FromBody] AddBoardRequest request)
    {
        var result = await BoardService.Create(request);

        return Ok(result);
    }

    // DELETE: api/boards/5
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json", Type = typeof(ClientResponse<BoardViewModel>))]
    public async Task<IActionResult> DeleteBoard([FromRoute] int id)
    {
        var result = await BoardService.Delete(id);

        if (result.IsNotFound) return NotFound();

        return Ok(result);
    }

    // GET: api/boards/is-unique
    [HttpGet("is-unique/{identifier}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json", Type = typeof(ClientResponse<IsSlugUniqueResponse>))]
    public async Task<IActionResult> IsSlugUnique([FromRoute] string identifier)
    {
        var result = await BoardService.IsIdentifierUnique(identifier);

        return Ok(result);
    }
}
