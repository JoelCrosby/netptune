using System.Text.Json.Serialization;

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

    public bool ActorIsServiceAccount { get; set; }

    public string? EntityName { get; set; }

    public string? EntityIdentifier { get; set; }

    public int? ActivityEntryId { get; set; }

    public int? RevisionCount { get; set; }

    [JsonIgnore]
    public string[]? ChangedFieldsArray { get; set; }

    public List<string> ChangedFields => ChangedFieldsArray is null ? [] : [.. ChangedFieldsArray];
}
