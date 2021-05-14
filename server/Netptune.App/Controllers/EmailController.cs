
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Authorization;
using Netptune.Core.Messaging;
using Netptune.Core.Models.Messaging;

namespace Netptune.App.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = NetptunePolicies.Workspace)]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService Email;

        public EmailController(IEmailService email)
        {
            Email = email;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SendGrid()
        {
            await Email.Send(new SendEmailModel
            {
                Reason = "Test Hangfire",
                Name = "Joel Crosby",
                Message = "Send Controller Test Message.",
                SendTo = new SendTo
                {
                    Address = "joelcrosby94@gmail.com",
                    DisplayName = "Joel Crosby",
                },
            });

            return Ok();
        }
    }
}
