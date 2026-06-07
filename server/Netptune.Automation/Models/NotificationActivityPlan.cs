using Netptune.Core.Entities;

namespace Netptune.Automation.Models;

internal sealed record NotificationActivityPlan(
    PendingAutomationExecution Execution,
    ActivityLog Activity,
    List<string> RecipientUserIds);
