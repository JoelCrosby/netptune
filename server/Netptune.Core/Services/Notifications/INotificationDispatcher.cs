using Netptune.Core.Enums;

namespace Netptune.Core.Services.Notifications;

public sealed record NotificationDispatchRequest
{
    public required string UserId { get; init; }

    public required string ActorUserId { get; init; }

    public required long EventRecordId { get; init; }

    public required int WorkspaceId { get; init; }

    public EntityType EntityType { get; init; }

    public ActivityType ActivityType { get; init; }
}

public interface INotificationDispatcher
{
    Task Dispatch(NotificationDispatchRequest request, CancellationToken cancellationToken = default);
}
