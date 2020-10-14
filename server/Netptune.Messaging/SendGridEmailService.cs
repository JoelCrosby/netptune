using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly SendGridEmailOptions Options;

        public SendGridEmailService(IOptionsMonitor<SendGridEmailOptions> options, IEmailRenderService emailRenderer)
        {
            EmailRenderer = emailRenderer;
            Options = options.CurrentValue;
        }

        public async Task Send(SendEmailModel model)
        {
            var client = new SendGridClient(Options.SendGridApiKey);

            var subject = model.Subject;

            var from = new EmailAddress(Options.DefaultFromAddress, Options.DefaultFromDisplayName);
            var to = new EmailAddress(model.SendTo.Address, model.SendTo.DisplayName);

            var plainTextContent = model.RawTextContent;
            var htmlContent = await EmailRenderer.Render(model);

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

            var response = await client.SendEmailAsync(msg);

            if ((int)response.StatusCode > 399)
            {
                // TODO: Request failed Do something...
            }
        }

        public async Task Send(IEnumerable<SendEmailModel> models)
        {
            var client = new SendGridClient(Options.SendGridApiKey);

            var modelList = models.ToList();

            var subjects = modelList.Select(model => model.Subject).ToList();

            var from = new EmailAddress(Options.DefaultFromAddress, Options.DefaultFromDisplayName);
            var to = modelList.Select(model => new EmailAddress(model.SendTo.Address, model.SendTo.DisplayName)).ToList();

            var firstEmail = modelList.FirstOrDefault() ?? throw new ArgumentNullException(nameof(modelList), "models enumerable was empty.");

            var substitutions = modelList.Select(model => new Dictionary<string, string>
            {
                {"-name-", model.Name},
                {"-action-", model.Action},
                {"-pre-header-", model.PreHeader},
                {"-link-", model.Link},
            }).ToList();

            var plainTextContent = firstEmail.RawTextContent;

            var htmlContent = await EmailRenderer.Render(new SendEmailModel
            {
                Message = firstEmail.Message,
                Subject = firstEmail.Subject,
                Reason = firstEmail.Reason,
                Name = "-name-",
                Action = "-action-",
                RawTextContent = firstEmail.RawTextContent,
                PreHeader = "-pre-header-",
                Link = "-link-",
            });

            var msg = MailHelper.CreateMultipleEmailsToMultipleRecipients(
                from,
                to,
                subjects,
                plainTextContent,
                htmlContent,
                substitutions);

            var response = await client.SendEmailAsync(msg);

            if ((int)response.StatusCode > 399)
            {
                // TODO: Request failed Do something...
            }
        }
    }
}
