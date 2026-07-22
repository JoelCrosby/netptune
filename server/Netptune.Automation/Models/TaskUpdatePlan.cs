using Netptune.Core.Enums;

namespace Netptune.Automation.Models;

internal sealed record TaskUpdatePlan
{
    public required PendingAutomationExecution Execution { get; init; }

    public int? StatusId { get; init; }

    public TaskPriority? Priority { get; init; }
}
