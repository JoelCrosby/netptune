using System.Threading.Tasks;

using Flurl;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Authentication;
using Netptune.Core.Authentication.Models;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;

using AuthenticationTicket = Netptune.Core.Authentication.Models.AuthenticationTicket;

namespace Netptune.App.Controllers
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

            // Encoding for plus symbols is going wonky some where ...
            var decodedCode = code.Replace(' ', '+');

            var result = await AuthenticationService.ConfirmEmail(userId, decodedCode);

            if (result is null) return Unauthorized();

            return Ok(result.Ticket);
        }

        // GET: api/auth/request-password-reset
        [HttpGet]
        [AllowAnonymous]
        [Route("request-password-reset")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json", Type = typeof(ClientResponse))]
        public async Task<IActionResult> RequestPasswordReset([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest();
            }

            var result = await AuthenticationService.RequestPasswordReset(new RequestPasswordResetRequest
            {
                Email = email,
            });

            return Ok(result);
        }

        // GET: api/auth/reset-password
        [HttpGet]
        [AllowAnonymous]
        [Route("reset-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json", Type = typeof(AuthenticationTicket))]
        public async Task<IActionResult> ResetPassword([FromQuery] string userId, [FromQuery] string code, [FromQuery] string password)
        {
            if (string.IsNullOrWhiteSpace(userId)
                || string.IsNullOrWhiteSpace(code)
                || string.IsNullOrWhiteSpace(password)
            )
            {
                return Unauthorized();
            }

            // Encoding for plus symbols is going wonky some where ...
            var decodedCode = code.Replace(' ', '+');

            var result = await AuthenticationService.ResetPassword(new ResetPasswordRequest
            {
                Code = decodedCode,
                UserId = userId,
                Password = password,
            });

            if (result?.IsSuccess != true) return Unauthorized();

            return Ok(result.Ticket);
        }

        // GET: api/auth/change-password
        [HttpPatch]
        [Route("change-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json", Type = typeof(AuthenticationTicket))]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var result = await AuthenticationService.ChangePassword(request);

            if (result?.IsSuccess != true) return Unauthorized();

            return Ok(result);
        }

        // GET: api/auth/current-user
        [HttpGet]
        [AllowAnonymous]
        [Route("current-user")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json", Type = typeof(CurrentUserResponse))]
        public async Task<IActionResult> CurrentUser()
        {
            var result = await AuthenticationService.CurrentUser();

            if (result is null) return Unauthorized();

            return Ok(result);
        }

        // GET: api/auth/validate-workspace-invite
        [HttpGet]
        [AllowAnonymous]
        [Route("validate-workspace-invite")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json", Type = typeof(CurrentUserResponse))]
        public async Task<IActionResult> ValidateWorkspaceInvite([FromQuery] string code)
        {
            var result = await AuthenticationService.ValidateInviteCode(code);

            if (result is null) return Unauthorized();

            return Ok(result);
        }

        // GET: api/auth/github-login
        [HttpGet]
        [AllowAnonymous]
        [Route("github-login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GithubLogin()
        {
            return Challenge(new AuthenticationProperties
            {
                RedirectUri = "/api/auth/provider-login-redirect",
                IsPersistent = true,
            },"GitHub");
        }

        // GET: api/auth/provider-login-redirect
        [HttpGet, HttpPost]
        [Authorize(AuthenticationSchemes = "Identity.External")]
        [Route("provider-login-redirect")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json", Type = typeof(AuthenticationTicket))]
        public async Task<IActionResult> AuthProviderLogin()
        {
            var result = await AuthenticationService.LogInViaProvider();

            if (!result.IsSuccess) return Unauthorized();

            var redirect = "/auth/auth-provider-login".SetQueryParams(new
            {
                expires = result.Ticket.Expires,
                issued = result.Ticket.Issued,
                token = result.Ticket.Token,
                displayName = result.Ticket.DisplayName,
                email = result.Ticket.EmailAddress,
                pictureUrl = result.Ticket.PictureUrl,
                userId = result.Ticket.UserId,
            }).ToString();

            return Redirect(redirect);
        }
    }
}
