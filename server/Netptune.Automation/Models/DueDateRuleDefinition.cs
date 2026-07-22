using Netptune.Core.Entities;

namespace Netptune.Automation.Models;

internal sealed record DueDateRuleDefinition(AutomationRule Rule, int DurationDays);
