using System.Text.Json;

using Netptune.Core.Enums;
using Netptune.Core.Models;

namespace Netptune.Core.ViewModels.Activity;

public class ActivityViewModel
{
    public int Id { get; set; }

    public EntityType EntityType { get; set; }

    public string? UserId { get; set; }

    public string? UserUsername { get; set; }

    public string? UserPictureUrl { get; set; }

    public bool UserIsServiceAccount { get; set; }

    public string? Agent { get; set; }

    public ActivityType Type { get; set; }

    public int? EntityId { get; set; }

    public string? TaskSystemId { get; set; }

    public string? TaskName { get; set; }

    public DateTime Time { get; set; }

    public DateTime FirstTime { get; set; }

    public List<string> ChangedFields { get; set; } = [];

    public int RevisionCount { get; set; } = 1;

    public JsonDocument? Meta { get; set; }

    public UserAvatar Assignee { get; set; } = null!;
}
