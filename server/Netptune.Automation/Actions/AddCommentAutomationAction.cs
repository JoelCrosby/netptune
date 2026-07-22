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

internal sealed class AddCommentAutomationAction : IAutomationAction
{
    public AutomationActionType Type => AutomationActionType.AddComment;

    public string? Validate(AutomationActionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Comment))
        {
            return "Add comment actions require comment.";
        }

        return request.Comment.Length > 32768 ? "Add comment actions cannot exceed 32768 characters." : null;
    }

    public JsonDocument CreateConfig(AutomationActionRequest request)
    {
        return JsonSerializer.SerializeToDocument(new
        {
            comment = request.Comment?.Trim(),
        }, JsonOptions.Default);
    }

    public AutomationActionViewModel ToViewModel(AutomationAction action)
    {
        return new AutomationActionViewModel
        {
            Id = action.Id,
            Type = action.Type,
            SortOrder = action.SortOrder,
            Comment = ConfigReader.ReadString(action.Config, "comment"),
        };
    }

    public AutomationActionPlanContribution Plan(AutomationActionPlanningContext context)
    {
        var body = ConfigReader.ReadString(context.Action.Config, "comment")?.Trim();

        return new AutomationActionPlanContribution
        {
            CommentBody = string.IsNullOrWhiteSpace(body) ? null : body,
        };
    }
}
