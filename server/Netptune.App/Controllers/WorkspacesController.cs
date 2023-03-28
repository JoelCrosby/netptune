using System.Collections.Generic;
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

namespace Netptune.App.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WorkspacesController : ControllerBase
{
    private readonly IWorkspaceService WorkspaceService;
    private readonly IAuthorizationService AuthorizationService;

    public WorkspacesController(IWorkspaceService workspaceService, IAuthorizationService authorizationService)
    {
        WorkspaceService = workspaceService;
        AuthorizationService = authorizationService;
    }

    private Task<AuthorizationResult> AuthorizeWorkspace(string workspaceKey)
    {
        return AuthorizationService.AuthorizeAsync(User, workspaceKey, NetptunePolicies.Workspace);
    }

    // GET: api/Workspaces
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json", Type = typeof(List<Workspace>))]
    public async Task<IActionResult> GetWorkspaces()
    {
        var result = await WorkspaceService.GetUserWorkspaces();

        return Ok(result);
    }

    // GET: api/workspaces/key
    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json", Type = typeof(Workspace))]
    public async Task<IActionResult> GetWorkspace([FromRoute] string key)
    {
        var authorizationResult = await AuthorizeWorkspace(key);
        if (!authorizationResult.Succeeded) return Forbid();

        var result = await WorkspaceService.GetWorkspace(key);

        if (result is null) return NotFound();

        return Ok(result);
    }

    // PUT: api/workspaces/5
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json", Type = typeof(Workspace))]
    public async Task<IActionResult> PutWorkspace([FromBody] UpdateWorkspaceRequest request)
    {
        var authorizationResult = await AuthorizeWorkspace(request.Slug!);
        if (!authorizationResult.Succeeded) return Forbid();

        var result = await WorkspaceService.Update(request);

        return Ok(result);
    }

    // POST: api/Workspaces
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json", Type = typeof(Workspace))]
    public async Task<IActionResult> PostWorkspace([FromBody] AddWorkspaceRequest request)
    {
        var result = await WorkspaceService.Create(request);

        return Ok(result);
    }

    // DELETE: api/workspaces/key
    [HttpDelete("{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json", Type = typeof(Workspace))]
    public async Task<IActionResult> DeleteWorkspace([FromRoute] string key)
    {
        var authorizationResult = await AuthorizeWorkspace(key);
        if (!authorizationResult.Succeeded) return Forbid();

        var result = await WorkspaceService.Delete(key);

        if (result.IsNotFound) return NotFound();

        return Ok(result);
    }

    // DELETE: api/workspaces/key
    [HttpDelete("permanent/{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json", Type = typeof(Workspace))]
    public async Task<IActionResult> DeleteWorkspacePermanent([FromRoute] string key)
    {
        var authorizationResult = await AuthorizeWorkspace(key);
        if (!authorizationResult.Succeeded) return Forbid();

        var result = await WorkspaceService.DeletePermanent(key);

        if (result.IsNotFound) return NotFound();

        return Ok(result);
    }

    // GET: api/workspaces/all
    [HttpGet("all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json", Type = typeof(List<Workspace>))]
    public async Task<IActionResult> GetAllWorkspaces()
    {
        var result = await WorkspaceService.GetAll();

        return Ok(result);
    }

    // GET: api/workspaces/is-unique
    [HttpGet("is-unique/{slug}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json", Type = typeof(ClientResponse<IsSlugUniqueResponse>))]
    public async Task<IActionResult> IsSlugUnique([FromRoute] string slug)
    {
        var result = await WorkspaceService.IsSlugUnique(slug);

        return Ok(result);
    }
}
