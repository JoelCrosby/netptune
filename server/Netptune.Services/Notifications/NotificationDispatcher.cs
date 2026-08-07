using Microsoft.Extensions.Logging;

using Netptune.Core.Entities;
using Netptune.Core.Services.Notifications;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Notifications;

public sealed class NotificationDispatcher : INotificationDispatcher
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly INotificationEventPublisher NotificationEvents;
    private readonly ILogger<NotificationDispatcher> Logger;

    public NotificationDispatcher(
        INetptuneUnitOfWork unitOfWork,
        INotificationEventPublisher notificationEvents,
        ILogger<NotificationDispatcher> logger)
    {
        UnitOfWork = unitOfWork;
        NotificationEvents = notificationEvents;
        Logger = logger;
    }

    public async Task Dispatch(NotificationDispatchRequest request, CancellationToken cancellationToken = default)
    {
        var workspaceUserIds = await UnitOfWork.WorkspaceUsers.GetWorkspaceUserIds(request.WorkspaceId, cancellationToken);

        var recipients = await NotificationRecipientResolver.Resolve(
            UnitOfWork,
            new NotificationRecipientRequest
            {
                RequestedUserIds = [request.UserId],
                WorkspaceUserIds = workspaceUserIds,
                ActorUserId = request.ActorUserId,
                WorkspaceId = request.WorkspaceId,
                ActivityType = request.ActivityType,
                ExcludeActor = false,
            },
            cancellationToken);

        if (recipients.Count == 0)
        {
            return;
        }

        var notifications = recipients.Select(userId => new Notification
        {
            UserId = userId,
            EventRecordId = request.EventRecordId,
            IsRead = false,
            WorkspaceId = request.WorkspaceId,
            EntityType = request.EntityType,
            ActivityType = request.ActivityType,
            CreatedByUserId = request.ActorUserId,
            OwnerId = request.ActorUserId,
        }).ToList();

        await UnitOfWork.Notifications.AddRangeAsync(notifications, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        Logger.LogInformation(
            "Dispatching {NotificationCount} {ActivityType} notifications for workspace {WorkspaceId}",
            notifications.Count,
            request.ActivityType,
            request.WorkspaceId);

        var events = notifications.Select(notification =>
            new UserNotificationEvent(
                notification.UserId,
                new NotificationEvent(notification.Id, false)));

        await NotificationEvents.PublishManyAsync(events, cancellationToken);
    }
}
