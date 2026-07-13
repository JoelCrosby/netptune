using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using Netptune.Core.Encoding;
using Netptune.Core.Enums;

namespace Netptune.Core.Models.Activity;

public class ActivityChangeSetOptions
{
    [Required]
    public int? EntityId { get; set; }

    [Required]
    public int? WorkspaceId { get; set; }

    [Required]
    public EntityType EntityType { get; set; }

    [Required]
    public string UserId { get; set; } = null!;

    public List<string>? RecipientUserIds { get; set; }

    public List<ActivityChange> Changes { get; } = [];

    public void Add(ActivityType type, TaskChangeField field, string? oldValue, string? newValue)
    {
        Changes.Add(new ActivityChange
        {
            Type = type,
            Field = field,
            OldValue = oldValue,
            NewValue = newValue,
        });
    }

    public void AddWith<TMeta>(ActivityType type, TMeta meta) where TMeta : class
    {
        Changes.Add(new ActivityChange
        {
            Type = type,
            Meta = JsonSerializer.Serialize(meta, JsonOptions.Default),
        });
    }
}

public sealed class ActivityChange
{
    public ActivityType Type { get; init; }

    public TaskChangeField? Field { get; init; }

    public string? OldValue { get; init; }

    public string? NewValue { get; init; }

    public string? Meta { get; init; }
}
