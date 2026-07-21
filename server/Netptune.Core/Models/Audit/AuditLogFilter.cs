using Netptune.Core.Enums;
using Netptune.Core.Requests;

namespace Netptune.Core.Models.Audit;

public class AuditLogFilter : PageRequest
{
    public string? UserId { get; set; }

    public EntityType? EntityType { get; set; }

    public ActivityType? ActivityType { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }
}
