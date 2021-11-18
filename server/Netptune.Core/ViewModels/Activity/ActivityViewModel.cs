using System;
using System.Text.Json;

using Netptune.Core.Enums;
using Netptune.Core.Models;

namespace Netptune.Core.ViewModels.Activity;

public class ActivityViewModel
{
    public EntityType EntityType { get; set; }

    public string UserId { get; set; }

    public string UserUsername { get; set; }

    public string UserPictureUrl { get; set; }

    public ActivityType Type { get; set; }

    public int? EntityId { get; set; }

    public DateTime Time { get; set; }

    public JsonDocument Meta { get; set; }

    public UserAvatar Assignee { get; set; }
}