using System.Text;

using Mediator;

using Microsoft.Extensions.Logging;

using Netptune.Core.Entities;
using Netptune.Core.Services.Activity;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Webhooks;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.SourceControl.Webhooks;

public enum SourceControlWebhookOutcome
{
    Accepted = 0,
    Duplicate = 1,
    Ignored = 2,
    InvalidSignature = 3,
    Unroutable = 4,
    NotEnabled = 5,
}

public sealed record ReceiveSourceControlWebhookCommand(SourceControlProvider Provider, ScmWebhookRequest Request)
    : IRequest<SourceControlWebhookOutcome>;

public sealed class ReceiveSourceControlWebhookCommandHandler
    : IRequestHandler<ReceiveSourceControlWebhookCommand, SourceControlWebhookOutcome>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ISourceControlProviderRegistry Registry;
    private readonly IEventPublisher EventPublisher;
    private readonly ILogger<ReceiveSourceControlWebhookCommandHandler> Logger;

    public ReceiveSourceControlWebhookCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        ISourceControlProviderRegistry registry,
        IEventPublisher eventPublisher,
        ILogger<ReceiveSourceControlWebhookCommandHandler> logger)
    {
        UnitOfWork = unitOfWork;
        Registry = registry;
        EventPublisher = eventPublisher;
        Logger = logger;
    }

    public async ValueTask<SourceControlWebhookOutcome> Handle(
        ReceiveSourceControlWebhookCommand command,
        CancellationToken cancellationToken)
    {
        var isEnabled = Registry.IsEnabled(command.Provider);
        var reader = isEnabled ? Registry.Resolve(command.Provider).WebhookReader : null;

        if (reader is null)
        {
            return SourceControlWebhookOutcome.NotEnabled;
        }

        var verification = reader.Verify(command.Request, null);

        if (verification != ScmWebhookVerification.Verified)
        {
            Logger.LogWarning("Rejected a {Provider} webhook: {Verification}", command.Provider, verification);

            return SourceControlWebhookOutcome.InvalidSignature;
        }

        var envelope = reader.ReadEnvelope(command.Request);

        if (envelope is null)
        {
            return SourceControlWebhookOutcome.Unroutable;
        }

        // GitHub's ping and installs for accounts nobody has connected carry nothing to act on.
        if (envelope.ExternalAccountId is null)
        {
            return SourceControlWebhookOutcome.Ignored;
        }

        var isConnected = await UnitOfWork.SourceControlConnections.HasExternalAccount(
            command.Provider,
            envelope.ExternalAccountId,
            cancellationToken);

        if (!isConnected)
        {
            return SourceControlWebhookOutcome.Ignored;
        }

        var delivery = new ScmWebhookDelivery
        {
            Provider = command.Provider,
            DeliveryId = envelope.DeliveryId,
            EventName = envelope.EventName,
            Action = envelope.Action,
            ExternalAccountId = envelope.ExternalAccountId,
            Body = Encoding.UTF8.GetString(envelope.Body.Span),
            ReceivedAt = envelope.ReceivedAt.UtcDateTime,
        };

        var deliveryRecordId = await UnitOfWork.ScmWebhookDeliveries.AddIfNew(delivery, cancellationToken);

        if (deliveryRecordId is null)
        {
            return SourceControlWebhookOutcome.Duplicate;
        }

        await Publish(deliveryRecordId.Value);

        return SourceControlWebhookOutcome.Accepted;
    }

    // The delivery is already stored, so a failed publish is picked up by the maintenance sweep rather
    // than failing the request and making GitHub redeliver.
    private async Task Publish(long deliveryRecordId)
    {
        try
        {
            await EventPublisher.Dispatch(new ScmWebhookReceivedMessage { DeliveryRecordId = deliveryRecordId });
        }
        catch (Exception exception)
        {
            Logger.LogWarning(exception, "Publishing webhook delivery {DeliveryRecordId} failed", deliveryRecordId);
        }
    }
}
