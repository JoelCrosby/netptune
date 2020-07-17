using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Netptune.Core.Messaging;
using Netptune.Core.Models.Messaging;

using SendGrid;
using SendGrid.Helpers.Mail;

namespace Netptune.Messaging
{
    public class SendGridEmailService : IEmailService
    {
        private readonly IEmailRenderService EmailRenderer;
        private readonly EmailOptions Options;
        private readonly string SendGridAPIKey;

        public SendGridEmailService(IOptionsMonitor<EmailOptions> options, IEmailRenderService emailRenderer)
        {
            EmailRenderer = emailRenderer;
            Options = options.CurrentValue;

            SendGridAPIKey = Environment.GetEnvironmentVariable("SEND_GRID_API_KEY");

            if (string.IsNullOrEmpty(SendGridAPIKey))
            {
                throw new Exception("Unable to get API key for SendGrid. Environment variable SEND_GRID_API_KEY does not exist.");
            }
        }

        public async Task Send(SendEmailModel model)
        {
            var client = new SendGridClient(SendGridAPIKey);

            var subject = model.Subject;

            var from = new EmailAddress(Options.DefaultFromAddress, Options.DefaultFromDisplayName);
            var to = new EmailAddress(model.ToAddress, model.ToDisplayName);

            var plainTextContent = model.RawTextContent;
            var htmlContent = await EmailRenderer.Render(model);

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

            var response = await client.SendEmailAsync(msg);

            if ((int)response.StatusCode > 399)
            {
                // TODO: Request failed Do something...
            }
        }
    }
}
