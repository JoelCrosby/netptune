namespace Netptune.Core.Models.Automations;

public sealed record AutomationRunStats(int RuleId, int RunCount, int FailureCount);
