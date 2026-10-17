using Mediator;

using Netptune.Core.SourceControl.Webhooks;

namespace Netptune.JobServer.Handlers;

public sealed class SourceControlWebhookHandler(IScmWebhookProcessor processor)
    : IRequestHandler<ScmWebhookReceivedMessage>
{
    public async ValueTask<Unit> Handle(ScmWebhookReceivedMessage request, CancellationToken cancellationToken)
    {
        await processor.Process(request.DeliveryRecordId, cancellationToken);

        return Unit.Value;
    }
}
