using System.Text;

using Microsoft.Extensions.Logging;

using Netptune.Core.Entities;
using Netptune.Core.Services.Realtime;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Webhooks;
using Netptune.Core.UnitOfWork;

namespace Netptune.SourceControl.Webhooks;

internal sealed class ScmWebhookProcessor : IScmWebhookProcessor
{
    private const int MaxErrorLength = 2000;

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ISourceControlProviderRegistry Registry;
    private readonly IEnumerable<IScmEventHandler> Handlers;
    private readonly IWorkspaceEventPublisher WorkspaceEvents;
    private readonly TimeProvider Clock;
    private readonly ILogger<ScmWebhookProcessor> Logger;

    public ScmWebhookProcessor(
        INetptuneUnitOfWork unitOfWork,
        ISourceControlProviderRegistry registry,
        IEnumerable<IScmEventHandler> handlers,
        IWorkspaceEventPublisher workspaceEvents,
        TimeProvider clock,
        ILogger<ScmWebhookProcessor> logger)
    {
        UnitOfWork = unitOfWork;
        Registry = registry;
        Handlers = handlers;
        WorkspaceEvents = workspaceEvents;
        Clock = clock;
        Logger = logger;
    }

    public async Task Process(long deliveryRecordId, CancellationToken cancellationToken)
    {
        var delivery = await UnitOfWork.ScmWebhookDeliveries.GetAsync(
            deliveryRecordId,
            cancellationToken: cancellationToken);
        var isPending = delivery is not null && delivery.ProcessedAt is null;

        if (!isPending)
        {
            return;
        }

        try
        {
            var changedWorkspaces = await Apply(delivery!, cancellationToken);

            delivery!.ProcessedAt = Clock.GetUtcNow().UtcDateTime;
            delivery.LastError = null;

            await UnitOfWork.CompleteAsync(cancellationToken);
            await NotifyWorkspaces(changedWorkspaces);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Logger.LogWarning(exception, "Processing webhook delivery {DeliveryRecordId} failed", deliveryRecordId);

            delivery!.Attempts++;
            delivery.LastError = exception.Message.Length > MaxErrorLength
                ? exception.Message[..MaxErrorLength]
                : exception.Message;

            await UnitOfWork.CompleteAsync(CancellationToken.None);

            throw;
        }
    }

    private async Task<HashSet<string>> Apply(ScmWebhookDelivery delivery, CancellationToken cancellationToken)
    {
        var changedWorkspaces = new HashSet<string>();
        var isEnabled = Registry.IsEnabled(delivery.Provider);

        if (!isEnabled || delivery.ExternalAccountId is null)
        {
            return changedWorkspaces;
        }

        var reader = Registry.Resolve(delivery.Provider).WebhookReader;

        if (reader is null)
        {
            return changedWorkspaces;
        }

        var envelope = new ScmWebhookEnvelope
        {
            Provider = delivery.Provider,
            DeliveryId = delivery.DeliveryId,
            EventName = delivery.EventName,
            Action = delivery.Action,
            ExternalAccountId = delivery.ExternalAccountId,
            ReceivedAt = new DateTimeOffset(delivery.ReceivedAt, TimeSpan.Zero),
            Body = Encoding.UTF8.GetBytes(delivery.Body),
        };

        var connections = await UnitOfWork.SourceControlConnections.GetByExternalAccount(
            delivery.Provider,
            delivery.ExternalAccountId,
            cancellationToken);

        foreach (var connection in connections)
        {
            var scmEvents = reader.Normalise(envelope, connection.ToScmConnection());

            foreach (var scmEvent in scmEvents)
            {
                var handler = Handlers.FirstOrDefault(candidate => candidate.CanHandle(scmEvent));

                if (handler is null)
                {
                    continue;
                }

                var changed = await handler.Handle(scmEvent, connection, cancellationToken);

                if (changed)
                {
                    changedWorkspaces.Add(connection.Workspace!.Slug);
                }
            }
        }

        return changedWorkspaces;
    }

    private async Task NotifyWorkspaces(IEnumerable<string> workspaces)
    {
        foreach (var workspace in workspaces)
        {
            await WorkspaceEvents.PublishAsync(workspace, string.Empty, [WorkspaceEventScopes.SourceControl]);
        }
    }
}
