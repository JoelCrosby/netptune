using Netptune.Core.Entities;
using Netptune.Core.Enums;

namespace Netptune.Automation.Models;

internal sealed record AutomationPersistencePlan
{
    public required AutomationTriggerType TriggerType { get; init; }

    public required List<AutomationRun> Runs { get; init; }

    public required List<NotificationActivityPlan> NotificationPlans { get; init; }

    public required List<Flag> Flags { get; init; }

    public required List<TaskUpdatePlan> TaskUpdatePlans { get; init; }

    public required List<CommentPlan> CommentPlans { get; init; }
}
