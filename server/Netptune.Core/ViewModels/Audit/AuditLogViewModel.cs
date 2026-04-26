using System.Text.Json;

using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Audit;

public class AuditLogViewModel
{
    public int Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public string UserId { get; set; } = null!;
    public string UserDisplayName { get; set; } = null!;
    public string? UserPictureUrl { get; set; }
    public ActivityType Type { get; set; }
    public EntityType EntityType { get; set; }
    public int? EntityId { get; set; }
    public string? WorkspaceSlug { get; set; }
    public string? ProjectSlug { get; set; }
    public string? BoardSlug { get; set; }
    public JsonDocument? Meta { get; set; }
}
