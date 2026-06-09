using Netptune.Core.Entities;
using Netptune.Core.Enums;

namespace Netptune.Automation.Models;

internal sealed record TaskUpdatePlan
{
    public required PendingAutomationExecution Execution { get; init; }

    public required AutomationAction Action { get; init; }

    public ProjectTaskStatus? Status { get; init; }

    public TaskPriority? Priority { get; init; }
}
