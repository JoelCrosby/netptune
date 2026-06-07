using System.Text.Json;
using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

public record AutomationAction : AuditableEntity<int>
{
    public int AutomationRuleId { get; set; }

    public AutomationActionType Type { get; set; }

    public int SortOrder { get; set; }

    public JsonDocument? Config { get; set; }

    [JsonIgnore]
    public AutomationRule AutomationRule { get; set; } = null!;
}
