using Netptune.Core.Entities;

namespace Netptune.Automation.Models;

internal sealed record UnassignedRuleDefinition(AutomationRule Rule, int DurationDays);
