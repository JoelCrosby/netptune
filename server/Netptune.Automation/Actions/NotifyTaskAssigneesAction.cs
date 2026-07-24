using System.Text.Json;

using Netptune.Core.Authorization;
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
    private static readonly List<AutomationNotificationRecipient> DefaultRecipients = [AutomationNotificationRecipient.Assignees];

    public AutomationActionType Type => AutomationActionType.NotifyTaskAssignees;

    public IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>
    {
        NetptunePermissions.Tasks.Read,
    };

    public string? Validate(AutomationActionRequest request)
    {
        var messageError = AutomationMessageTemplate.Validate(request.Message);

        if (messageError is not null)
        {
            return messageError;
        }

        var hasUndefinedRecipient = request.Recipients.Any(recipient => !Enum.IsDefined(recipient));

        if (hasUndefinedRecipient)
        {
            return "Notify actions contain an unsupported recipient.";
        }

        var hasDuplicateRecipients = request.Recipients.Distinct().Count() != request.Recipients.Count;

        if (hasDuplicateRecipients)
        {
            return "Notify actions cannot repeat a recipient.";
        }

        var targetsSpecificUsers = request.Recipients.Contains(AutomationNotificationRecipient.SpecificUsers);
        var hasValidUserIds = request.RecipientUserIds.Count > 0
            && request.RecipientUserIds.All(userId => !string.IsNullOrWhiteSpace(userId))
            && request.RecipientUserIds.Distinct(StringComparer.Ordinal).Count() == request.RecipientUserIds.Count;

        if (targetsSpecificUsers && !hasValidUserIds)
        {
            return "Notify actions require distinct users when notifying specific users.";
        }

        var targetsRoles = request.Recipients.Contains(AutomationNotificationRecipient.WorkspaceRoles);
        var hasValidRoles = request.RecipientRoles.Count > 0
            && request.RecipientRoles.All(Enum.IsDefined)
            && request.RecipientRoles.Distinct().Count() == request.RecipientRoles.Count;

        if (targetsRoles && !hasValidRoles)
        {
            return "Notify actions require distinct workspace roles when notifying by role.";
        }

        return null;
    }

    public JsonDocument CreateConfig(AutomationActionRequest request)
    {
        var recipients = request.Recipients.Count > 0 ? request.Recipients : DefaultRecipients;
        var targetsSpecificUsers = recipients.Contains(AutomationNotificationRecipient.SpecificUsers);
        var targetsRoles = recipients.Contains(AutomationNotificationRecipient.WorkspaceRoles);

        List<string> recipientUserIds = targetsSpecificUsers ? request.RecipientUserIds : [];
        List<WorkspaceRole> recipientRoles = targetsRoles ? request.RecipientRoles : [];

        return JsonSerializer.SerializeToDocument(new
        {
            message = request.Message,
            recipients,
            recipientUserIds,
            recipientRoles,
        }, JsonOptions.Default);
    }

    public AutomationActionViewModel ToViewModel(AutomationAction action)
    {
        return new AutomationActionViewModel
        {
            Id = action.Id,
            Type = action.Type,
            SortOrder = action.SortOrder,
            Message = JsonUtils.ReadString(action.Config, "message"),
            Recipients = ReadRecipients(action),
            RecipientUserIds = JsonUtils.ReadList<string>(action.Config, "recipientUserIds"),
            RecipientRoles = JsonUtils.ReadEnumList<WorkspaceRole>(action.Config, "recipientRoles"),
        };
    }

    public AutomationActionPlanContribution Plan(AutomationActionPlanningContext context)
    {
        var task = context.Task;
        var rule = context.Rule;
        var recipients = ReadRecipients(context.Action);
        var recipientIds = ResolveDirectRecipients(context, recipients);
        var includeProjectMembers = recipients.Contains(AutomationNotificationRecipient.ProjectMembers) && task.ProjectId.HasValue;

        List<WorkspaceRole> roles = recipients.Contains(AutomationNotificationRecipient.WorkspaceRoles)
            ? JsonUtils.ReadEnumList<WorkspaceRole>(context.Action.Config, "recipientRoles")
            : [];

        var hasAudience = recipientIds.Count > 0 || includeProjectMembers || roles.Count > 0;

        if (!hasAudience)
        {
            return new AutomationActionPlanContribution();
        }

        var configuredMessage = JsonUtils.ReadString(context.Action.Config, "message");
        var notificationMessage = configuredMessage is null
            ? $"Automation '{rule.Name}' matched this task."
            : AutomationMessageTemplate.Render(configuredMessage, task, rule);

        var activity = CreateActivity(context, notificationMessage);

        return new AutomationActionPlanContribution
        {
            Notification = new AutomationNotificationContribution
            {
                Activity = activity,
                RecipientUserIds = recipientIds,
                IncludeProjectMembers = includeProjectMembers,
                RecipientRoles = roles,
            },
        };
    }

    private static List<AutomationNotificationRecipient> ReadRecipients(AutomationAction action)
    {
        var configured = JsonUtils.ReadEnumList<AutomationNotificationRecipient>(action.Config, "recipients");

        return configured.Count > 0 ? configured : DefaultRecipients;
    }

    private static List<string> ResolveDirectRecipients(
        AutomationActionPlanningContext context,
        List<AutomationNotificationRecipient> recipients)
    {
        var task = context.Task;
        var recipientIds = new List<string>();

        foreach (var recipient in recipients)
        {
            switch (recipient)
            {
                case AutomationNotificationRecipient.Assignees:
                    recipientIds.AddRange(task.ProjectTaskAppUsers.Select(assignee => assignee.UserId));
                    break;
                case AutomationNotificationRecipient.TaskOwner:
                    recipientIds.Add(task.OwnerId!);
                    break;
                case AutomationNotificationRecipient.TriggeringUser:
                    recipientIds.Add(context.InitiatingUserId!);
                    break;
                case AutomationNotificationRecipient.SpecificUsers:
                    recipientIds.AddRange(JsonUtils.ReadList<string>(context.Action.Config, "recipientUserIds"));
                    break;
            }
        }

        var hasAssigneeFallback = recipients.Contains(AutomationNotificationRecipient.Assignees)
            && recipients.Count == 1
            && task.ProjectTaskAppUsers.Count == 0;

        if (hasAssigneeFallback)
        {
            recipientIds.Add(task.OwnerId!);
        }

        return recipientIds
            .Where(userId => !string.IsNullOrWhiteSpace(userId))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static EventRecord CreateActivity(AutomationActionPlanningContext context, string notificationMessage)
    {
        var task = context.Task;
        var rule = context.Rule;

        return new EventRecord
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
    }
}
