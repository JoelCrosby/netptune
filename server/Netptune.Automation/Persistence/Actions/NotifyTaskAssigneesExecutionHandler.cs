using System.Text.Json;

using Netptune.Automation.Models;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Notifications;

namespace Netptune.Automation.Persistence.Actions;

internal sealed class NotifyTaskAssigneesExecutionHandler : IAutomationActionExecutionHandler
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public NotifyTaskAssigneesExecutionHandler(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public AutomationActionType Type => AutomationActionType.NotifyTaskAssignees;

    public async Task<AutomationActionExecutionOutcome> Execute(
        PlannedAutomationAction action,
        AutomationPersistenceState state,
        CancellationToken cancellationToken)
    {
        var contribution = action.Contribution.Notification;

        if (contribution is null)
        {
            return AutomationActionExecutionOutcomes.InvalidContribution();
        }

        await UnitOfWork.EventRecords.AddRangeAsync([contribution.Activity], cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        var actionNotifications = await BuildNotifications(action, contribution, cancellationToken);

        await UnitOfWork.Notifications.AddRangeAsync(actionNotifications, cancellationToken);
        state.Notifications.AddRange(actionNotifications);
        action.Result.Output = JsonSerializer.SerializeToDocument(new
        {
            notificationCount = actionNotifications.Count,
        }, JsonOptions.Default);

        return AutomationActionExecutionOutcomes.Succeeded();
    }

    private async Task<List<Notification>> BuildNotifications(
        PlannedAutomationAction action,
        AutomationNotificationContribution contribution,
        CancellationToken cancellationToken)
    {
        var task = action.Execution.Task;
        var actorUserId = action.Execution.ActorUserId;
        var recipients = await NotificationRecipientResolver.Resolve(
            UnitOfWork,
            new NotificationRecipientRequest
            {
                RequestedUserIds = contribution.RecipientUserIds,
                WorkspaceUserIds = contribution.RecipientUserIds,
                ActorUserId = actorUserId,
                WorkspaceId = task.WorkspaceId,
                ActivityType = ActivityType.Modify,
                ExcludeActor = false,
            },
            cancellationToken);
        var link = BuildTaskLink(task);
        var notifications = recipients.Select(userId => new Notification
        {
            UserId = userId,
            EventRecordId = contribution.Activity.Id,
            IsRead = false,
            Link = link,
            WorkspaceId = task.WorkspaceId,
            EntityType = EntityType.Task,
            ActivityType = ActivityType.Modify,
            CreatedByUserId = actorUserId,
            OwnerId = actorUserId,
        }).ToList();

        return notifications;
    }

    private static string BuildTaskLink(ProjectTask task)
    {
        var identifier = task.Project is null
            ? task.Id.ToString()
            : $"{task.Project.Key}-{task.ProjectScopeId}";

        return $"/{task.Workspace.Slug}/tasks/{identifier}";
    }
}
