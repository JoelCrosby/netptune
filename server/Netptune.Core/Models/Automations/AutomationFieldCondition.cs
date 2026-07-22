using Netptune.Core.Enums;

namespace Netptune.Core.Models.Automations;

public sealed record AutomationFieldCondition(TaskChangeField Field, AutomationConditionOperator Operator, string? Value);
