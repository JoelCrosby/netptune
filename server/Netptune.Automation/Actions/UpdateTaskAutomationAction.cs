using System.Text.Json;

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
        var statusId = JsonUtils.ReadInt(action.Config, "statusId");
        var priority = JsonUtils.ReadEnum<TaskPriority>(action.Config, "priority");

        return new AutomationActionViewModel
        {
            Id = action.Id,
            Type = action.Type,
            SortOrder = action.SortOrder,
            StatusId = statusId,
            Priority = priority,
        };
    }

    public AutomationActionPlanContribution Plan(AutomationActionPlanningContext context)
    {
        var statusId = JsonUtils.ReadInt(context.Action.Config, "statusId");
        var priority = JsonUtils.ReadEnum<TaskPriority>(context.Action.Config, "priority");
        var hasTaskUpdate = statusId is not null || priority is not null;
        var taskUpdate = hasTaskUpdate
            ? new AutomationTaskUpdateContribution(statusId, priority)
            : null;

        return new AutomationActionPlanContribution
        {
            TaskUpdate = taskUpdate,
        };
    }
}
