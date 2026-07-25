using Netptune.Core.Enums;
using Netptune.Core.Requests;

namespace Netptune.Core.Models.Automations;

public class AutomationRuleFilter : PageRequest
{
    public string? Search { get; set; }

    public bool? IsEnabled { get; set; }

    public AutomationTriggerType? TriggerType { get; set; }
}
