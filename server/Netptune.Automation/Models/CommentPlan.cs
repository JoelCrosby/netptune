using Netptune.Core.Entities;

namespace Netptune.Automation.Models;

internal sealed record CommentPlan(PendingAutomationExecution Execution, AutomationAction Action, string Body);
