using System.Text.Json;

using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Models.Automations;
using Netptune.Core.Requests;
using Netptune.Core.Services.Automations;
using Netptune.Core.ViewModels.Automations;

namespace Netptune.Automation.Actions;

internal sealed class NotifyTaskAssigneesAction : IAutomationAction
{
    public AutomationActionType Type => AutomationActionType.NotifyTaskAssignees;

    public string? Validate(AutomationActionRequest request)
    {
        return null;
    }

    public JsonDocument CreateConfig(AutomationActionRequest request)
    {
        return JsonSerializer.SerializeToDocument(new
        {
            message = request.Message,
        }, JsonOptions.Default);
    }

    public AutomationActionViewModel ToViewModel(AutomationAction action)
    {
        var message = JsonUtils.ReadString(action.Config, "message");

        return new AutomationActionViewModel
        {
            Id = action.Id,
            Type = action.Type,
            SortOrder = action.SortOrder,
            Message = message,
        };
    }

    public AutomationActionPlanContribution Plan(AutomationActionPlanningContext context)
    {
        var task = context.Task;
        var rule = context.Rule;
        var recipientIds = task.ProjectTaskAppUsers
            .Select(assignee => assignee.UserId)
            .DefaultIfEmpty(task.OwnerId)
            .Where(userId => !string.IsNullOrWhiteSpace(userId))
            .Select(userId => userId!)
            .Distinct()
            .ToList();

        if (recipientIds.Count == 0)
        {
            return new AutomationActionPlanContribution();
        }

        var configuredMessage = JsonUtils.ReadString(context.Action.Config, "message");
        var notificationMessage = configuredMessage ?? $"Automation '{rule.Name}' matched this task.";

        var activity = new EventRecord
        {
            EventId = Guid.NewGuid(),
            WorkspaceId = task.WorkspaceId,
            EventKey = EventKeys.EntityActivityRecorded,
            SchemaVersion = 1,
            SubjectType = EventEntityTypes.From(EntityType.Task),
            SubjectId = task.Id.ToString(),
            OccurredAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow,
            ActorUserId = context.ActorUserId,
            RetentionClass = EventRetentionClasses.Audit,
            Payload = JsonSerializer.SerializeToDocument(new
            {
                activityType = (int)ActivityType.Modify,
                workspaceSlug = task.Workspace.Slug,
                projectSlug = task.Project?.Key,
                automationRuleId = rule.Id,
                automationRuleName = rule.Name,
                message = notificationMessage,
            }, JsonOptions.Default),
            References =
            [
                new EventReference
                {
                    Role = EventReferenceRoles.Scope,
                    EntityType = EventEntityTypes.From(EntityType.Project),
                    EntityId = task.ProjectId!.Value.ToString(),
                },
            ],
        };

        return new AutomationActionPlanContribution
        {
            Notification = new AutomationNotificationContribution(activity, recipientIds),
        };
    }
}
