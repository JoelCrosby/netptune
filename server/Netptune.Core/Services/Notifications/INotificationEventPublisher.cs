namespace Netptune.Core.Services.Notifications;

public sealed record NotificationEvent(int NotificationId, bool IsRead);

public sealed record UserNotificationEvent(string UserId, NotificationEvent Event);

public interface INotificationEventPublisher
{
    Task PublishAsync(
        string userId,
        NotificationEvent notificationEvent,
        CancellationToken cancellationToken = default);

    Task PublishManyAsync(
        IEnumerable<UserNotificationEvent> notificationEvents,
        CancellationToken cancellationToken = default);
}
