using System.Text.Json;
using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

public record AutomationRule : WorkspaceEntity<int>
{
    public string Name { get; set; } = null!;

    public bool IsEnabled { get; set; }

    public AutomationTriggerType TriggerType { get; set; }

    public JsonDocument? TriggerConfig { get; set; }

    [JsonIgnore]
    public ICollection<AutomationAction> Actions { get; set; } = new HashSet<AutomationAction>();

    [JsonIgnore]
    public ICollection<AutomationRun> Runs { get; set; } = new HashSet<AutomationRun>();
}
