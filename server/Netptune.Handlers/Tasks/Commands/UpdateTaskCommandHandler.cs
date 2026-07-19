using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Events;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Handlers.Tasks.Commands;

public sealed record UpdateTaskCommand(UpdateProjectTaskRequest Request) : IRequest<ClientResponse<TaskViewModel>>;

public sealed class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, ClientResponse<TaskViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IActivityLogger Activity;
    private readonly IEventPublisher EventPublisher;
    private readonly IIdentityService Identity;
    private readonly IEventRecordWriter EventRecords;

    public UpdateTaskCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IActivityLogger activity,
        IEventPublisher eventPublisher,
        IIdentityService identity,
        IEventRecordWriter eventRecords)
    {
        UnitOfWork = unitOfWork;
        Activity = activity;
        EventPublisher = eventPublisher;
        Identity = identity;
        EventRecords = eventRecords;
    }

    public async ValueTask<ClientResponse<TaskViewModel>> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var workspaceId = await Identity.GetWorkspaceId();
        var old = await UnitOfWork.Tasks.GetTaskViewModel(req.Id, cancellationToken);

        if (old is null || old.WorkspaceId != workspaceId)
        {
            return ClientResponse<TaskViewModel>.NotFound;
        }

        var result = await UnitOfWork.Tasks.GetTaskForUpdate(req.Id, cancellationToken);

        if (result is null)
        {
            return ClientResponse<TaskViewModel>.NotFound;
        }

        var startDate = req.StartDateSpecified ? req.StartDate : result.StartDate;
        var dueDate = req.DueDateSpecified ? req.DueDate : result.DueDate;
        var hasValidSchedule = ProjectTaskSchedule.IsValid(startDate, dueDate);

        if (!hasValidSchedule)
        {
            return ClientResponse<TaskViewModel>.Failed(ProjectTaskSchedule.InvalidDateRangeMessage);
        }

        var status = await ResolveStatus(req, workspaceId, cancellationToken);

        if (req.StatusId.HasValue && status is null)
        {
            return ClientResponse<TaskViewModel>.Failed($"Status with id {req.StatusId.Value} was not found in the workspace");
        }

        var assigneeIds = req.AssigneeIds?
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (req.AssigneeIds is not null)
        {
            var containsInvalidAssignee = assigneeIds!.Count != req.AssigneeIds.Count;

            if (containsInvalidAssignee)
            {
                return ClientResponse<TaskViewModel>.Failed("Assignee IDs cannot be empty or duplicated");
            }

            var assignees = await UnitOfWork.Users.IsUserInWorkspaceRange(
                assigneeIds,
                workspaceId,
                cancellationToken);
            var validAssigneeIds = assignees.Select(assignee => assignee.Id).ToHashSet(StringComparer.Ordinal);
            var missingAssigneeIds = assigneeIds.Where(id => !validAssigneeIds.Contains(id)).ToList();

            if (missingAssigneeIds.Count > 0)
            {
                return ClientResponse<TaskViewModel>.Failed(
                    $"Assignees were not found in the workspace: {string.Join(", ", missingAssigneeIds)}");
            }
        }

        var tagUpdate = await ResolveTagUpdate(req.Tags, workspaceId, cancellationToken);

        if (!tagUpdate.IsValid)
        {
            return ClientResponse<TaskViewModel>.Failed(tagUpdate.Error);
        }

        TaskViewModel? response = null;
        ProjectTaskDiff? diff = null;

        await UnitOfWork.Transaction(async () =>
        {
            if (status is not null && result.StatusId != status.Id)
            {
                result.StatusId = status.Id;
            }

            result.Name = req.Name ?? result.Name;
            result.Description = req.Description ?? result.Description;
            result.OwnerId = req.OwnerId ?? result.OwnerId;
            result.Priority = req.Priority ?? result.Priority;
            result.EstimateType = req.EstimateType ?? result.EstimateType;
            result.EstimateValue = req.EstimateValue ?? result.EstimateValue;

            if (req.StartDateSpecified)
            {
                result.StartDate = req.StartDate;
            }

            if (req.DueDateSpecified)
            {
                result.DueDate = req.DueDate;
            }

            if (req.AssigneeIds is not null)
            {
                result.ProjectTaskAppUsers = ProjectTaskAppUser.MergeUsersIds(
                    result.Id,
                    result.ProjectTaskAppUsers,
                    assigneeIds!).ToList();
            }

            if (tagUpdate.ShouldUpdate)
            {
                result.ProjectTaskTags = ProjectTaskTag.MergeTagIds(
                    result.Id,
                    result.ProjectTaskTags,
                    tagUpdate.Tags.Select(tag => tag.Id)).ToList();
            }

            await UnitOfWork.CompleteAsync(cancellationToken);

            response = await UnitOfWork.Tasks.GetTaskViewModel(result.Id, cancellationToken);

            if (response is null)
            {
                return;
            }

            diff = ProjectTaskDiff.Create(old, response);

            await AppendReportingEvents(
                old,
                response,
                diff,
                cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);
        });

        if (response is null)
        {
            return ClientResponse<TaskViewModel>.NotFound;
        }

        if (diff is null)
        {
            return ClientResponse<TaskViewModel>.Success(response);
        }

        diff.LogDiff(Activity, response.Id);

        if (diff.HasChanges && response.WorkspaceId is not null)
        {
            await PublishTaskChanged(response, diff);
        }

        return ClientResponse<TaskViewModel>.Success(response);
    }

    private async Task AppendReportingEvents(
        TaskViewModel old,
        TaskViewModel updated,
        ProjectTaskDiff diff,
        CancellationToken cancellationToken)
    {
        foreach (var change in diff.ToTaskFieldChanges())
        {
            var payload = new FieldTransitionedPayload
            {
                Field = change.Field.ToString().ToLowerInvariant(),
                OldValue = change.OldValue,
                NewValue = change.NewValue,
                OldCategory = change.Field == TaskChangeField.Status ? old.StatusCategory.ToString() : null,
                NewCategory = change.Field == TaskChangeField.Status ? updated.StatusCategory.ToString() : null,
                OldUnit = change.Field == TaskChangeField.Estimate ? old.EstimateType?.ToString() : null,
                NewUnit = change.Field == TaskChangeField.Estimate ? updated.EstimateType?.ToString() : null,
                OldNumericValue = change.Field == TaskChangeField.Estimate ? old.EstimateValue : null,
                NewNumericValue = change.Field == TaskChangeField.Estimate ? updated.EstimateValue : null,
            };

            var references = new List<EventReferenceInput>();
            var projectId = updated.ProjectId ?? old.ProjectId;

            if (projectId.HasValue)
            {
                references.Add(new EventReferenceInput
                {
                    Role = EventReferenceRoles.Scope,
                    EntityType = EventEntityTypes.From(EntityType.Project),
                    EntityId = projectId.Value.ToString(),
                });
            }

            if (updated.SprintId.HasValue)
            {
                references.Add(new EventReferenceInput
                {
                    Role = EventReferenceRoles.Scope,
                    EntityType = EventEntityTypes.From(EntityType.Sprint),
                    EntityId = updated.SprintId.Value.ToString(),
                });
            }

            await EventRecords.Append(new EventWriteRequest<FieldTransitionedPayload>
            {
                WorkspaceId = updated.WorkspaceId,
                EventKey = EventKeys.EntityFieldTransitioned,
                SubjectType = EventEntityTypes.From(EntityType.Task),
                SubjectId = updated.Id.ToString(),
                Payload = payload,
                References = references,
            }, cancellationToken);

            if (change.Field == TaskChangeField.Estimate && updated.SprintId.HasValue && updated.SprintStatus == SprintStatus.Active)
            {
                await EventRecords.Append(new EventWriteRequest<ScopeMemberAttributeChangedPayload>
                {
                    WorkspaceId = updated.WorkspaceId,
                    EventKey = EventKeys.ScopeMemberAttributeChanged,
                    SubjectType = EventEntityTypes.From(EntityType.Sprint),
                    SubjectId = updated.SprintId.Value.ToString(),
                    Payload = new ScopeMemberAttributeChangedPayload
                    {
                        MemberType = EventEntityTypes.From(EntityType.Task),
                        MemberId = updated.Id.ToString(),
                        Field = "estimate",
                        OldUnit = old.EstimateType?.ToString(),
                        NewUnit = updated.EstimateType?.ToString(),
                        OldNumericValue = old.EstimateValue,
                        NewNumericValue = updated.EstimateValue,
                    },
                    References =
                    [
                        new EventReferenceInput
                        {
                            Role = EventReferenceRoles.Member,
                            EntityType = EventEntityTypes.From(EntityType.Task),
                            EntityId = updated.Id.ToString(),
                        },
                        ..references,
                    ],
                }, cancellationToken);
            }
        }
    }

    private Task PublishTaskChanged(TaskViewModel current, ProjectTaskDiff diff)
    {
        return EventPublisher.Dispatch(new TaskChangedMessage
        {
            WorkspaceId = current.WorkspaceId!.Value,
            TaskId = current.Id,
            ActorUserId = Identity.GetCurrentUserId(),
            Changes = diff.ToTaskFieldChanges(),
        });
    }

    private async Task<TagUpdateResolution> ResolveTagUpdate(
        IReadOnlyCollection<string>? requestedTags,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        if (requestedTags is null)
        {
            return new TagUpdateResolution(false, [], string.Empty);
        }

        var tagNames = requestedTags
            .Select(tag => tag.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var containsInvalidTag = tagNames.Count != requestedTags.Count;

        if (containsInvalidTag)
        {
            return new TagUpdateResolution(false, [], "Tags cannot be empty or duplicated");
        }

        var tags = await UnitOfWork.Tags.GetTagsByValueInWorkspace(
            workspaceId,
            tagNames,
            true,
            cancellationToken);
        var foundTagNames = tags.Select(tag => tag.Name).ToHashSet(StringComparer.Ordinal);
        var missingTags = tagNames.Where(tag => !foundTagNames.Contains(tag)).ToList();

        if (missingTags.Count > 0)
        {
            var error = $"Tags were not found in the workspace: {string.Join(", ", missingTags)}";

            return new TagUpdateResolution(false, [], error);
        }

        return new TagUpdateResolution(true, tags, string.Empty);
    }

    private async Task<Status?> ResolveStatus(UpdateProjectTaskRequest request, int workspaceId, CancellationToken cancellationToken)
    {

        if (request.StatusId.HasValue)
        {
            var status = await UnitOfWork.Statuses.GetInWorkspace(request.StatusId.Value, workspaceId, cancellationToken: cancellationToken);

            return status;
        }

        return null;
    }

    private sealed record TagUpdateResolution(
        bool ShouldUpdate,
        IReadOnlyList<Tag> Tags,
        string Error)
    {
        public bool IsValid => Error.Length == 0;
    }
}
