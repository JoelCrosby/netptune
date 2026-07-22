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

internal sealed class DeleteTaskAutomationAction : IAutomationAction
{
    public AutomationActionType Type => AutomationActionType.DeleteTask;

    public string? Validate(AutomationActionRequest request)
    {
        var delayAmount = request.DelayAmount ?? 0;

        if (delayAmount is < 0 or > 525600)
        {
            return "Delete task action delay must be between 0 and 525600.";
        }

        if (delayAmount > 0 && request.DelayUnit is null)
        {
            return "Delete task actions with a delay require delayUnit.";
        }

        var delay = CreateDelay(delayAmount, request.DelayUnit ?? AutomationDelayUnit.Minutes);

        if (delay > TimeSpan.FromDays(365))
        {
            return "Delete task action delay cannot exceed 365 days.";
        }

        return null;
    }

    public JsonDocument CreateConfig(AutomationActionRequest request)
    {
        return JsonSerializer.SerializeToDocument(new
        {
            delayAmount = request.DelayAmount ?? 0,
            delayUnit = request.DelayUnit ?? AutomationDelayUnit.Minutes,
        }, JsonOptions.Default);
    }

    public AutomationActionViewModel ToViewModel(AutomationAction action)
    {
        return new AutomationActionViewModel
        {
            Id = action.Id,
            Type = action.Type,
            SortOrder = action.SortOrder,
            DelayAmount = ConfigReader.ReadInt(action.Config, "delayAmount") ?? 0,
            DelayUnit = ConfigReader.ReadEnum<AutomationDelayUnit>(action.Config, "delayUnit") ?? AutomationDelayUnit.Minutes,
        };
    }

    public AutomationActionPlanContribution Plan(AutomationActionPlanningContext context)
    {
        var delayAmount = ConfigReader.ReadInt(context.Action.Config, "delayAmount") ?? 0;
        var delayUnit = ConfigReader.ReadEnum<AutomationDelayUnit>(context.Action.Config, "delayUnit") ?? AutomationDelayUnit.Minutes;
        var delay = CreateDelay(delayAmount, delayUnit);

        return new AutomationActionPlanContribution
        {
            TaskDeletion = new AutomationTaskDeletionContribution(delay),
        };
    }

    private static TimeSpan CreateDelay(int amount, AutomationDelayUnit unit)
    {
        return unit switch
        {
            AutomationDelayUnit.Minutes => TimeSpan.FromMinutes(amount),
            AutomationDelayUnit.Hours => TimeSpan.FromHours(amount),
            AutomationDelayUnit.Days => TimeSpan.FromDays(amount),
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported automation delay unit."),
        };
    }
}
