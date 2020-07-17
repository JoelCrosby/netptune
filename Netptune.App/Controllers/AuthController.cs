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
        private readonly INetptuneAuthService AuthenticationService;

        public AuthController(INetptuneAuthService authenticationService)
        {
            AuthenticationService = authenticationService;
        }

        // POST: api/auth/login
        [HttpPost]
        [AllowAnonymous]
        [Route("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json", Type = typeof(AuthenticationTicket))]
        public async Task<IActionResult> Login([FromBody] TokenRequest model)
        {
            var result = await AuthenticationService.LogIn(model);

            if (!result.IsSuccess) return Unauthorized();

            return Ok(result.Ticket);
        }

        // POST: api/auth/register
        [HttpPost]
        [AllowAnonymous]
        [Route("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json", Type = typeof(AuthenticationTicket))]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            var result = await AuthenticationService.Register(model);

            if (!result.IsSuccess) return Unauthorized(result.Message);

            return Ok(result.Ticket);
        }

        // GET: api/auth/confirm-email
        [HttpGet]
        [AllowAnonymous]
        [Route("confirm-email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json", Type = typeof(AuthenticationTicket))]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string code)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
            {
                return Unauthorized();
            }

            var result = await AuthenticationService.ConfirmEmail(userId, code);

            if (!result.Succeeded) return Unauthorized();

            return Ok(result);
        }
    }
}
