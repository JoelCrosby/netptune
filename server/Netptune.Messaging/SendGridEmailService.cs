using Microsoft.Extensions.Options;

using Netptune.Core.Messaging;
using Netptune.Core.Models.Messaging;
using Netptune.Core.Services.Activity;

using SendGrid;
using SendGrid.Helpers.Mail;

namespace Netptune.Messaging;

public class SendGridEmailService : IEmailService
{
    private readonly IEmailRenderService EmailRenderer;
    private readonly IEventPublisher EventPublisher;
    private readonly SendGridEmailOptions Options;

    public SendGridEmailService(
        IOptionsMonitor<SendGridEmailOptions> options,
        IEmailRenderService emailRenderer,
        IEventPublisher eventPublisher)
    {
        EmailRenderer = emailRenderer;
        EventPublisher = eventPublisher;
        Options = options.CurrentValue;
    }

    public Task Send(SendEmailModel model)
    {
        EventPublisher.Dispatch(model);

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

    public Task Send(SendMultipleEmailModel model)
    {
        EventPublisher.Dispatch(model);

        return Task.CompletedTask;
    }

    public async Task EnqueueSend(SendMultipleEmailModel model)
    {
        var client = new SendGridClient(Options.SendGridApiKey);

        var subjects = new List<string> { model.Subject };

        var from = new EmailAddress(Options.DefaultFromAddress, Options.DefaultFromDisplayName);

        var substitution = new Dictionary<string, string>
        {
            {"-name-", model.Name},
            {"-action-", model.Action ?? String.Empty},
            {"-pre-header-", model.PreHeader ?? String.Empty},
            {"-link-", model.Link ?? String.Empty},
        };

        var substitutions = new List<Dictionary<string, string>>
        {
            substitution,
        };

        var plainTextContent = model.RawTextContent;

        var to = model.SendTo.Select(t => new EmailAddress(t.Address, t.DisplayName)).ToList();

        var htmlContent = await EmailRenderer.Render(new SendEmailModel
        {
            Message = model.Message,
            Subject = model.Subject,
            Reason = model.Reason,
            Name = "-name-",
            Action = "-action-",
            RawTextContent = model.RawTextContent,
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
