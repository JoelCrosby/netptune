using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Netptune.Core.Jobs;
using Netptune.Core.Messaging;
using Netptune.Core.Models.Messaging;

using SendGrid;
using SendGrid.Helpers.Mail;

namespace Netptune.Messaging
{
    public class SendGridEmailService : IEmailService
    {
        private readonly IEmailRenderService EmailRenderer;
        private readonly IJobClient JobClient;
        private readonly SendGridEmailOptions Options;

        public SendGridEmailService(
            IOptionsMonitor<SendGridEmailOptions> options,
            IEmailRenderService emailRenderer,
            IJobClient jobClient)
        {
            EmailRenderer = emailRenderer;
            JobClient = jobClient;
            Options = options.CurrentValue;
        }

        public Task Send(SendEmailModel model)
        {
            JobClient.Enqueue<IEmailService>(service => service.EnqueueSend(model));

            return Task.CompletedTask;
        }

        public async Task EnqueueSend(SendEmailModel model)
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

        public Task Send(IEnumerable<SendEmailModel> models)
        {
            JobClient.Enqueue<IEmailService>(service => service.EnqueueSend(models));

            return Task.CompletedTask;
        }

        public async Task EnqueueSend(IEnumerable<SendEmailModel> models)
        {
            var client = new SendGridClient(Options.SendGridApiKey);

            var modelList = models.ToList();

            var subjects = modelList.ConvertAll(model => model.Subject);

            var from = new EmailAddress(Options.DefaultFromAddress, Options.DefaultFromDisplayName);
            var to = modelList.ConvertAll(model => new EmailAddress(model.SendTo.Address, model.SendTo.DisplayName));

            var firstEmail = modelList.FirstOrDefault() ?? throw new ArgumentNullException(nameof(modelList), "models enumerable was empty.");

            var substitutions = modelList.ConvertAll(model => new Dictionary<string, string>
            {
                {"-name-", model.Name},
                {"-action-", model.Action},
                {"-pre-header-", model.PreHeader},
                {"-link-", model.Link},
            });

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
