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
    ConnectionGone = 6,
}

public sealed record ReceiveSourceControlWebhookCommand(
    SourceControlProvider Provider,
    ScmWebhookRequest Request,
    int? ConnectionId = null) : IRequest<SourceControlWebhookOutcome>;

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

        if (command.ConnectionId is { } connectionId)
        {
            var outcome = await ReceiveForConnection(command, reader, connectionId, cancellationToken);

            return outcome;
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

        var delivery = CreateDelivery(command.Provider, envelope, envelope.DeliveryId, null);
        var stored = await Store(delivery, cancellationToken);

        return stored;
    }

    // The connection is found before verifying, because its own secret signs the request. Anything it
    // can't use is acknowledged rather than rejected: providers stop delivering to a hook that errors.
    private async Task<SourceControlWebhookOutcome> ReceiveForConnection(
        ReceiveSourceControlWebhookCommand command,
        ISourceControlWebhookReader reader,
        int connectionId,
        CancellationToken cancellationToken)
    {
        var connection = await UnitOfWork.SourceControlConnections.GetForWebhook(
            command.Provider,
            connectionId,
            cancellationToken);

        if (connection is null)
        {
            return SourceControlWebhookOutcome.ConnectionGone;
        }

        var verification = reader.Verify(command.Request, connection.ToScmConnection());

        if (verification != ScmWebhookVerification.Verified)
        {
            Logger.LogWarning(
                "Rejected a {Provider} webhook for connection {ConnectionId}: {Verification}",
                command.Provider,
                connectionId,
                verification);

            return SourceControlWebhookOutcome.InvalidSignature;
        }

        var envelope = reader.ReadEnvelope(command.Request);
        var isForConnection = envelope is not null && envelope.ExternalAccountId == connection.ExternalAccountId;

        if (!isForConnection)
        {
            Logger.LogWarning(
                "Ignored a {Provider} webhook that connection {ConnectionId} can't route",
                command.Provider,
                connectionId);

            return SourceControlWebhookOutcome.Ignored;
        }

        // One account connected in two workspaces has a webhook in each, and both carry the same event id.
        var deliveryId = $"{connection.Id}:{envelope!.DeliveryId}";
        var delivery = CreateDelivery(command.Provider, envelope, deliveryId, connection.Id);
        var stored = await Store(delivery, cancellationToken);

        return stored;
    }

    private static ScmWebhookDelivery CreateDelivery(
        SourceControlProvider provider,
        ScmWebhookEnvelope envelope,
        string deliveryId,
        int? connectionId)
    {
        return new ScmWebhookDelivery
        {
            Provider = provider,
            DeliveryId = deliveryId,
            EventName = envelope.EventName,
            Action = envelope.Action,
            ExternalAccountId = envelope.ExternalAccountId,
            ConnectionId = connectionId,
            Body = Encoding.UTF8.GetString(envelope.Body.Span),
            ReceivedAt = envelope.ReceivedAt.UtcDateTime,
        };
    }

    private async Task<SourceControlWebhookOutcome> Store(ScmWebhookDelivery delivery, CancellationToken cancellationToken)
    {
        var deliveryRecordId = await UnitOfWork.ScmWebhookDeliveries.AddIfNew(delivery, cancellationToken);

        if (deliveryRecordId is null)
        {
            // GitHub's "Redeliver" reuses the delivery id, so a delivery that never processed gets
            // another full set of attempts rather than being dropped as a duplicate.
            var revivedId = await UnitOfWork.ScmWebhookDeliveries.ReviveUnprocessed(
                delivery.Provider,
                delivery.DeliveryId,
                cancellationToken);

            if (revivedId is not null)
            {
                await Publish(revivedId.Value);
            }

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
