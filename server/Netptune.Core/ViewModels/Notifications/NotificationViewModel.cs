using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Notifications;

public class NotificationViewModel
{
    public int Id { get; set; }

    public bool IsRead { get; set; }

    public string? Link { get; set; }

    public EntityType EntityType { get; set; }

    public ActivityType ActivityType { get; set; }

    public DateTime CreatedAt { get; set; }

    public string ActorUserId { get; set; } = null!;

    public string ActorUsername { get; set; } = null!;

    public string? ActorPictureUrl { get; set; }
}
