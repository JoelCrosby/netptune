using System.Text.Json;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.Requests;
using Netptune.Core.ViewModels.Automations;

namespace Netptune.Core.Services.Automations;

public interface IAutomationAction
{
    AutomationActionType Type { get; }

    string? Validate(AutomationActionRequest request);

    JsonDocument CreateConfig(AutomationActionRequest request);

    AutomationActionViewModel ToViewModel(AutomationAction action);

    AutomationActionPlanContribution Plan(AutomationActionPlanningContext context);
}

public interface IAutomationActionRegistry
{
    IAutomationAction? Find(AutomationActionType type);
}
