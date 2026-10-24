using Microsoft.Extensions.Logging;
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
    private readonly ILogger<SendGridEmailService> Logger;

    public SendGridEmailService(
        IOptionsMonitor<SendGridEmailOptions> options,
        IEmailRenderService emailRenderer,
        IEventPublisher eventPublisher,
        ILogger<SendGridEmailService> logger)
    {
        EmailRenderer = emailRenderer;
        EventPublisher = eventPublisher;
        Options = options.CurrentValue;
        Logger = logger;
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
        var sendFailed = (int)response.StatusCode > 399;

        if (sendFailed)
        {
            var body = await response.Body.ReadAsStringAsync();

            Logger.LogError("SendGrid rejected an email with {StatusCode}: {Body}", response.StatusCode, body);
        }
    }
}
