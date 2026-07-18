using Netptune.Core.Entities;

namespace Netptune.Automation.Models;

internal sealed record NotificationActivityPlan
{
    public required PendingAutomationExecution Execution { get; init; }

    public required EventRecord Activity { get; init; }

    public required List<string> RecipientUserIds { get; init; }
}
