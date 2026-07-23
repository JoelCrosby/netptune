using System.Text.Json;

using Netptune.Core.Authorization;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.Requests;
using Netptune.Core.Services.Automations;
using Netptune.Core.ViewModels.Automations;

namespace Netptune.Automation.Actions;

internal sealed class FlagTaskAction : IAutomationAction
{
    public AutomationActionType Type => AutomationActionType.FlagTask;

    public IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>
    {
        NetptunePermissions.Tasks.Update,
    };

    public string? Validate(AutomationActionRequest request)
    {
        return string.IsNullOrWhiteSpace(request.FlagName) ? "Flag task actions require flagName." : null;
    }

    public JsonDocument CreateConfig(AutomationActionRequest request)
    {
        return JsonSerializer.SerializeToDocument(new
        {
            flagName = request.FlagName,
            flagDescription = request.FlagDescription,
        }, JsonOptions.Default);
    }

    public AutomationActionViewModel ToViewModel(AutomationAction action)
    {
        var flagName = JsonUtils.ReadString(action.Config, "flagName");
        var flagDescription = JsonUtils.ReadString(action.Config, "flagDescription");

        return new AutomationActionViewModel
        {
            Id = action.Id,
            Type = action.Type,
            SortOrder = action.SortOrder,
            FlagName = flagName,
            FlagDescription = flagDescription,
        };
    }

    public AutomationActionPlanContribution Plan(AutomationActionPlanningContext context)
    {
        var rule = context.Rule;
        var name = JsonUtils.ReadString(context.Action.Config, "flagName") ?? "Automation flag";
        var description = JsonUtils.ReadString(context.Action.Config, "flagDescription") ?? $"Flagged by automation '{rule.Name}'.";

        return new AutomationActionPlanContribution
        {
            Flag = new AutomationFlagContribution(name, description),
        };
    }
}
