using Netptune.Core.Enums;

namespace Netptune.Core.Models.Audit;

public class AuditLogFilter
{
    public int WorkspaceId { get; set; }
    public string? UserId { get; set; }
    public EntityType? EntityType { get; set; }
    public ActivityType? ActivityType { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
