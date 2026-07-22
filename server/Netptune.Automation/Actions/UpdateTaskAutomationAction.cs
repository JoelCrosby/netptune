using System.Text.Json;

using Netptune.Automation.Configuration;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.Requests;
using Netptune.Core.Services.Automations;
using Netptune.Core.ViewModels.Automations;

namespace Netptune.Automation.Actions;

internal sealed class UpdateTaskAutomationAction : IAutomationAction
{
    public AutomationActionType Type => AutomationActionType.UpdateTask;

    public string? Validate(AutomationActionRequest request)
    {
        return request.StatusId is null && request.Priority is null
            ? "Update task actions require status or priority."
            : null;
    }

    public JsonDocument CreateConfig(AutomationActionRequest request)
    {
        return JsonSerializer.SerializeToDocument(new
        {
            statusId = request.StatusId,
            priority = request.Priority,
        }, JsonOptions.Default);
    }

    public AutomationActionViewModel ToViewModel(AutomationAction action)
    {
        return new AutomationActionViewModel
        {
            Id = action.Id,
            Type = action.Type,
            SortOrder = action.SortOrder,
            StatusId = ConfigReader.ReadInt(action.Config, "statusId"),
            Priority = ConfigReader.ReadEnum<TaskPriority>(action.Config, "priority"),
        };
    }

    public AutomationActionPlanContribution Plan(AutomationActionPlanningContext context)
    {
        var statusId = ConfigReader.ReadInt(context.Action.Config, "statusId");
        var priority = ConfigReader.ReadEnum<TaskPriority>(context.Action.Config, "priority");

        return new AutomationActionPlanContribution
        {
            TaskUpdate = statusId is null && priority is null
                ? null
                : new AutomationTaskUpdateContribution(statusId, priority),
        };
    }
}
