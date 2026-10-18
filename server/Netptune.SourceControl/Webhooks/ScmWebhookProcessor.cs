using System.Runtime.ExceptionServices;
using System.Text;

using Microsoft.Extensions.Logging;

using Netptune.Core.Entities;
using Netptune.Core.Services.Activity;
using Netptune.Core.Services.Realtime;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Events;
using Netptune.Core.SourceControl.Models;
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
    private readonly IEventPublisher EventPublisher;
    private readonly ScmAutomationEvents AutomationEvents;
    private readonly TimeProvider Clock;
    private readonly ILogger<ScmWebhookProcessor> Logger;

    public ScmWebhookProcessor(
        INetptuneUnitOfWork unitOfWork,
        ISourceControlProviderRegistry registry,
        IEnumerable<IScmEventHandler> handlers,
        IWorkspaceEventPublisher workspaceEvents,
        IEventPublisher eventPublisher,
        ScmAutomationEvents automationEvents,
        TimeProvider clock,
        ILogger<ScmWebhookProcessor> logger)
    {
        UnitOfWork = unitOfWork;
        Registry = registry;
        Handlers = handlers;
        WorkspaceEvents = workspaceEvents;
        EventPublisher = eventPublisher;
        AutomationEvents = automationEvents;
        Clock = clock;
        Logger = logger;
    }

    public async Task Process(long deliveryRecordId, CancellationToken cancellationToken)
    {
        var delivery = await UnitOfWork.ScmWebhookDeliveries.GetAsync(
            deliveryRecordId,
            cancellationToken: cancellationToken);

        if (delivery is not { ProcessedAt: null })
        {
            return;
        }

        HashSet<(string Workspace, string Scope)> changes;

        AutomationEvents.Clear();

        try
        {
            changes = await Apply(delivery, cancellationToken);

            // Dispatched before the processed mark is saved, so a crash in between redelivers the webhook
            // rather than losing the triggers; each run's idempotency key absorbs the repeat.
            foreach (var message in AutomationEvents.TakeAll())
            {
                await EventPublisher.Dispatch(message);
            }

            delivery.ProcessedAt = Clock.GetUtcNow().UtcDateTime;
            delivery.LastError = null;

            // Handlers don't save, so this one save commits the links, the activity events they produce
            // and the processed mark together: a redelivery after a failure can't record activity twice.
            await UnitOfWork.CompleteAsync(cancellationToken);
        }
        catch (Exception exception) when (exception.GetBaseException() is not OperationCanceledException)
        {
            var failure = exception.GetBaseException();

            Logger.LogWarning(failure, "Processing webhook delivery {DeliveryRecordId} failed", deliveryRecordId);

            // Written directly: the failed writes are still tracked, so saving through the context
            // would retry them and fail the same way, and the attempt would never be counted.
            var error = failure.Message.Length > MaxErrorLength
                ? failure.Message[..MaxErrorLength]
                : failure.Message;

            await UnitOfWork.ScmWebhookDeliveries.RecordFailure(delivery.Id, error, CancellationToken.None);

            ExceptionDispatchInfo.Capture(failure).Throw();

            throw;
        }

        await NotifyWorkspaces(changes);
    }

    private async Task<HashSet<(string Workspace, string Scope)>> Apply(
        ScmWebhookDelivery delivery,
        CancellationToken cancellationToken)
    {
        var changedWorkspaces = new HashSet<(string Workspace, string Scope)>();

        if (delivery.ExternalAccountId is null)
        {
            return changedWorkspaces;
        }

        // Thrown rather than skipped: the App only stores deliveries for enabled providers, so this
        // host is misconfigured, and marking the delivery processed would silently discard it.
        var isEnabled = Registry.IsEnabled(delivery.Provider);
        var reader = isEnabled ? Registry.Resolve(delivery.Provider).WebhookReader : null;

        if (reader is null)
        {
            throw new InvalidOperationException($"{delivery.Provider} webhooks are not enabled on this host.");
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

        var observedAccounts = new List<ScmIdentity>();

        foreach (var connection in connections)
        {
            var scmEvents = reader.Normalise(envelope, connection.ToScmConnection());

            observedAccounts.AddRange(scmEvents.SelectMany(ScmObservedAccounts.Of));

            foreach (var scmEvent in scmEvents)
            {
                var handler = Handlers.FirstOrDefault(candidate => candidate.CanHandle(scmEvent));

                if (handler is null)
                {
                    continue;
                }

                var changed = await handler.Handle(scmEvent, connection, ScmEventSource.Webhook, cancellationToken);

                if (changed)
                {
                    changedWorkspaces.Add((connection.Workspace!.Slug, handler.RefreshScope));
                }
            }
        }

        await UnitOfWork.UserSourceControlIdentities.RecordObserved(
            delivery.Provider,
            observedAccounts,
            Clock.GetUtcNow().UtcDateTime,
            cancellationToken);

        return changedWorkspaces;
    }

    // The delivery is already committed, so a failed refresh only leaves open views stale until their
    // next reload; failing here would make the broker redeliver a message that has nothing left to do.
    private async Task NotifyWorkspaces(IEnumerable<(string Workspace, string Scope)> changes)
    {
        foreach (var workspace in changes.GroupBy(change => change.Workspace))
        {
            var scopes = workspace.Select(change => change.Scope).ToArray();

            try
            {
                await WorkspaceEvents.PublishAsync(workspace.Key, string.Empty, scopes);
            }
            catch (Exception exception)
            {
                Logger.LogWarning(exception, "Publishing source control changes to {Workspace} failed", workspace.Key);
            }
        }
    }
}
