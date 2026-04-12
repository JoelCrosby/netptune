using Mediator;

using Netptune.Core.Messaging;
using Netptune.Core.Models.Messaging;

namespace Netptune.JobServer.Handlers;

public class EmailHandler(IEmailService emailService) : IRequestHandler<SendEmailModel>
{
    public async ValueTask<Unit> Handle(SendEmailModel request, CancellationToken cancellationToken)
    {
        await emailService.EnqueueSend(request);

        return Unit.Value;
    }
}

public class EmailMultipleHandler(IEmailService emailService) : IRequestHandler<SendMultipleEmailModel>
{
    public async ValueTask<Unit> Handle(SendMultipleEmailModel request, CancellationToken cancellationToken)
    {
        await emailService.EnqueueSend(request);

        return Unit.Value;
    }
}
