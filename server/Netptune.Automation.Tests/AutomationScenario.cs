using Netptune.Core.Entities;

namespace Netptune.Automation.Tests;

internal sealed record AutomationScenario(
    Workspace Workspace,
    Project Project,
    ProjectTask Task,
    AppUser Owner,
    AppUser Assignee);
