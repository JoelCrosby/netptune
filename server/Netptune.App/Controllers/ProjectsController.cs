using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.ViewModels.Projects;

namespace Netptune.App.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = NetptunePolicies.Workspace)]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService ProjectService;

    public ProjectsController(IProjectService projectService)
    {
        ProjectService = projectService;
    }

    // GET: api/projects
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json", Type = typeof(List<ProjectViewModel>))]
    public async Task<IActionResult> GetProjects()
    {
        var result = await ProjectService.GetProjects();

        return Ok(result);
    }

    // GET: api/projects/key
    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json", Type = typeof(ProjectViewModel))]
    public async Task<IActionResult> GetProject([FromRoute] string key)
    {
        var result = await ProjectService.GetProject(key);

        if (result is null) return NotFound();

        return Ok(result);
    }

    // PUT: api/projects
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json", Type = typeof(ProjectViewModel))]
    public async Task<IActionResult> PutProject([FromBody] UpdateProjectRequest project)
    {
        var result = await ProjectService.Update(project);

        if (result.IsNotFound) return NotFound();

        return Ok(result);
    }

    // POST: api/projects
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json", Type = typeof(ProjectViewModel))]
    public async Task<IActionResult> PostProject([FromBody] AddProjectRequest request)
    {
        var result = await ProjectService.Create(request);

        return Ok(result);
    }

    // DELETE: api/projects/5
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProject([FromRoute] int id)
    {
        var result = await ProjectService.Delete(id);

        if (result.IsNotFound) return NotFound();

        return Ok(result);
    }
}
