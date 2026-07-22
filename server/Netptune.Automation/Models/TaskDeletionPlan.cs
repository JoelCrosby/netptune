using Netptune.Core.Entities;

namespace Netptune.Automation.Models;

internal sealed record TaskDeletionPlan(PendingAutomationExecution Execution, AutomationAction Action, TimeSpan Delay);
