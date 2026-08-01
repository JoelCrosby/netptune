using System.Text.Json;

using Netptune.Automation.Common;
using Netptune.Automation.Models;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Notifications;

namespace Netptune.Automation.Persistence.Actions;

internal sealed class NotifyTaskAssigneesHandler : IActionExecutionHandler
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public NotifyTaskAssigneesHandler(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public AutomationActionType Type => AutomationActionType.NotifyTaskAssignees;

    public async Task<ActionOutcome> Execute(
        PlannedAutomationAction action,
        AutomationPersistenceState state,
        CancellationToken cancellationToken)
    {
        var contribution = action.Contribution.Notification;

        if (contribution is null)
        {
            return ActionOutcomes.InvalidContribution();
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

        return ActionOutcomes.Succeeded();
    }

    private async Task<List<Notification>> BuildNotifications(
        PlannedAutomationAction action,
        AutomationNotificationContribution contribution,
        CancellationToken cancellationToken)
    {
        var task = action.Execution.Task;
        var actorUserId = action.Execution.ExecutionUserId!;
        var audience = await ResolveAudience(task, contribution, cancellationToken);
        var recipients = await NotificationRecipientResolver.Resolve(
            UnitOfWork,
            new NotificationRecipientRequest
            {
                RequestedUserIds = audience,
                WorkspaceUserIds = audience,
                ActorUserId = actorUserId,
                WorkspaceId = task.WorkspaceId,
                ActivityType = ActivityType.Modify,
                ExcludeActor = false,
            },
            cancellationToken);
        var notifications = recipients.Select(userId => new Notification
        {
            UserId = userId,
            EventRecordId = contribution.Activity.Id,
            IsRead = false,
            WorkspaceId = task.WorkspaceId,
            EntityType = EntityType.Task,
            ActivityType = ActivityType.Modify,
            CreatedByUserId = actorUserId,
            OwnerId = actorUserId,
        }).ToList();

        return notifications;
    }

    private async Task<List<string>> ResolveAudience(
        ProjectTask task,
        AutomationNotificationContribution contribution,
        CancellationToken cancellationToken)
    {
        var audience = new List<string>(contribution.RecipientUserIds);

        if (contribution.IncludeProjectMembers && task.ProjectId.HasValue)
        {
            var memberIds = await UnitOfWork.Projects.GetProjectMemberIds(task.ProjectId.Value, cancellationToken);

            audience.AddRange(memberIds);
        }

        if (contribution.RecipientRoles.Count > 0)
        {
            var roleUserIds = await UnitOfWork.Users.GetWorkspaceUserIdsInRoles(
                task.WorkspaceId,
                contribution.RecipientRoles,
                cancellationToken);

            audience.AddRange(roleUserIds);
        }

        return audience.Distinct(StringComparer.Ordinal).ToList();
    }
}
