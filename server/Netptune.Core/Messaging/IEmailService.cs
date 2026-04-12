using Netptune.Core.Models.Messaging;

namespace Netptune.Core.Messaging;

public interface IEmailService
{
    Task Send(SendEmailModel model);

    Task Send(SendMultipleEmailModel models);

    Task EnqueueSend(SendEmailModel model);

    Task EnqueueSend(SendMultipleEmailModel model);
}
