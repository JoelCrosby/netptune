using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Authentication;
using Netptune.Core.Authentication.Models;

namespace Netptune.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly INetptuneAuthService _authenticationService;

        public AuthController(INetptuneAuthService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("Login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(AuthenticationTicket))]
        public async Task<IActionResult> Login([FromBody] TokenRequest model)
        {
            var result = await _authenticationService.LogIn(model);

            if (!result.IsSuccess) return Unauthorized(result.Message);

            return Ok(result.Ticket);
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("Register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(AuthenticationTicket))]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            var result = await _authenticationService.Register(model);

            if (!result.IsSuccess) return Unauthorized(result.Message);

            return Ok(result.Ticket);
        }
    }
}