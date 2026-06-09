using Netptune.Core.Entities;

namespace Netptune.Automation.Models;

internal sealed record ActionPlan
{
    public required List<AutomationRun> Runs { get; init; }

    public required List<NotificationActivityPlan> NotificationPlans { get; init; }

    public required List<FlagPlan> FlagPlans { get; init; }

    public required List<TaskUpdatePlan> TaskUpdatePlans { get; init; }
}
