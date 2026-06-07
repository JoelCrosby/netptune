using Netptune.Core.Entities;

namespace Netptune.Automation.Models;

internal sealed record ActionPlan(
    List<AutomationRun> Runs,
    List<NotificationActivityPlan> NotificationPlans,
    List<FlagPlan> FlagPlans);
