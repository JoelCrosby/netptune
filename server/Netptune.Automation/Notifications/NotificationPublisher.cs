using Microsoft.Extensions.Logging;

using Netptune.Automation.Diagnostics;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Services.Notifications;

namespace Netptune.Automation.Notifications;

internal sealed class NotificationPublisher
{
    private readonly INotificationEventPublisher NotificationEvents;
    private readonly ILogger<NotificationPublisher> Logger;

    public NotificationPublisher(
        INotificationEventPublisher notificationEvents,
        ILogger<NotificationPublisher> logger)
    {
        NotificationEvents = notificationEvents;
        Logger = logger;
    }

    internal async Task Publish(
        AutomationTriggerType triggerType,
        List<Notification> notifications,
        CancellationToken cancellationToken)
    {
        if (notifications.Count == 0) return;

        Logger.LogInformation(
            "Publishing {NotificationCount} automation notification events for trigger {TriggerType}",
            notifications.Count,
            triggerType);

        var events = notifications.Select(notification =>
            new UserNotificationEvent(
                notification.UserId,
                new NotificationEvent(notification.Id, false)));

        await NotificationEvents.PublishManyAsync(events, cancellationToken);

        Telemetry.RecordNotificationsPublished(triggerType, notifications.Count);

        Logger.LogInformation(
            "Published {NotificationCount} automation notification events for trigger {TriggerType}",
            notifications.Count,
            triggerType);
    }
}
