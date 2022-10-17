using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.ViewModels.Users;

namespace Netptune.App.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService UserService;

    public UsersController(IUserService userService)
    {
        UserService = userService;
    }

    // GET: api/users
    [HttpGet]
    [Authorize(Policy = NetptunePolicies.Workspace)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json", Type = typeof(List<UserViewModel>))]
    public async Task<IActionResult> GetWorkspaceUsersAsync()
    {
        var result = await UserService.GetWorkspaceUsers();

        if (result is null) return NotFound();

        return Ok(result);
    }

    // GET: api/users/<guid>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json", Type = typeof(UserViewModel))]
    public async Task<IActionResult> GetUserAsync([FromRoute] string id)
    {
        var result = await UserService.Get(id);

        if (result is null) return NotFound();

        return Ok(result);
    }

    // GET: api/users/<id>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json", Type = typeof(UserViewModel))]
    public async Task<IActionResult> UpdateUser([FromBody] AppUser user)
    {
        var result = await UserService.Update(user);

        if (result.IsNotFound) return NotFound();

        return Ok(result);
    }

    // POST: api/users/invite
    [HttpPost]
    [Route("invite")]
    [Authorize(Policy = NetptunePolicies.Workspace)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json", Type = typeof(ClientResponse))]
    public async Task<IActionResult> Invite(InviteUsersRequest request)
    {
        var result = await UserService.InviteUsersToWorkspace(request.EmailAddresses);

        return Ok(result);
    }

    // POST: api/users/remove
    [HttpPost]
    [Route("remove")]
    [Authorize(Policy = NetptunePolicies.Workspace)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json", Type = typeof(ClientResponse))]
    public async Task<IActionResult> RemoveUserFromWorkspace(InviteUsersRequest request)
    {
        var result = await UserService.RemoveUsersFromWorkspace(request.EmailAddresses);

        return Ok(result);
    }

    // GET: api/users/get-by-email
    [HttpGet]
    [Route("get-by-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json", Type = typeof(UserViewModel))]
    public async Task<IActionResult> GetUserByEmailAsync(string email)
    {
        var result = await UserService.GetByEmail(email);

        if (result is null) return NotFound();

        return Ok(result);
    }

    // GET: api/users/all
    [HttpGet]
    [Route("all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json", Type = typeof(List<UserViewModel>))]
    public async Task<IActionResult> GetAll()
    {
        var result = await UserService.GetAll();

        return Ok(result);
    }
}
