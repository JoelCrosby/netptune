using Netptune.Core.Entities;

namespace Netptune.Automation.Models;

internal sealed record SprintEndingSoonRuleDefinition(AutomationRule Rule, int DurationDays);

internal sealed record SprintEndingSoonCandidate(SprintEndingSoonRuleDefinition Definition, List<Sprint> Sprints);
