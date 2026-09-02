using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Utilities;

namespace Netptune.Core.Models.Automations;

public class AutomationRunFilter : PageRequest
{
    public string? Search { get; set; }

    public string? Statuses { get; set; }

    public string? TriggerTypes { get; set; }

    public IReadOnlyList<AutomationRunStatus> GetStatuses()
    {
        return EnumList.Parse<AutomationRunStatus>(Statuses);
    }

    public IReadOnlyList<AutomationTriggerType> GetTriggerTypes()
    {
        return EnumList.Parse<AutomationTriggerType>(TriggerTypes);
    }
}
