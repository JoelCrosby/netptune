using Netptune.Core.Entities;

namespace Netptune.Automation.Models;

internal sealed record PendingAutomationExecution(
    AutomationRule Rule,
    ProjectTask Task,
    string ActorUserId,
    string IdempotencyKey);
