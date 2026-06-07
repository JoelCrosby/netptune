using Netptune.Core.Entities;

namespace Netptune.Automation.Models;

internal sealed record FlagPlan(PendingAutomationExecution Execution, AutomationAction Action);
