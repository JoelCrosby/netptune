using Mediator;

using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.ProjectTasks;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Handlers.Tasks.Commands;

public sealed record UpdateTaskCommand(UpdateProjectTaskRequest Request) : IRequest<ClientResponse<TaskViewModel>>;

public sealed class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, ClientResponse<TaskViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly ITaskMutationPipeline TaskMutationPipeline;
    private readonly ITaskReferenceResolver ReferenceResolver;
    private readonly ITaskStatusResolver StatusResolver;

    public UpdateTaskCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        ITaskMutationPipeline taskMutationPipeline,
        ITaskReferenceResolver referenceResolver,
        ITaskStatusResolver statusResolver)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        TaskMutationPipeline = taskMutationPipeline;
        ReferenceResolver = referenceResolver;
        StatusResolver = statusResolver;
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

        var status = req.StatusId.HasValue
            ? await StatusResolver.ResolveRequested(req.StatusId.Value, workspaceId, cancellationToken)
            : null;

        if (req.StatusId.HasValue && status is null)
        {
            return ClientResponse<TaskViewModel>.Failed($"Status with id {req.StatusId.Value} was not found in the workspace");
        }

        var assigneeUpdate = await ReferenceResolver.ResolveAssignees(req.AssigneeIds, workspaceId, cancellationToken);

        if (!assigneeUpdate.IsValid)
        {
            return ClientResponse<TaskViewModel>.Failed(assigneeUpdate.Error);
        }

        var tagUpdate = await ReferenceResolver.ResolveTags(req.Tags, workspaceId, cancellationToken);

        if (!tagUpdate.IsValid)
        {
            return ClientResponse<TaskViewModel>.Failed(tagUpdate.Error);
        }

        TaskViewModel? response = null;
        TaskMutationOutcome? mutationOutcome = null;

        await UnitOfWork.Transaction(async () =>
        {
            TaskMutationPipeline.Apply(result, new TaskMutationValues(status, req.Priority));

            result.Name = req.Name ?? result.Name;
            result.Description = req.Description ?? result.Description;
            result.OwnerId = req.OwnerId ?? result.OwnerId;
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

            if (assigneeUpdate.ShouldUpdate)
            {
                result.ProjectTaskAppUsers = ProjectTaskAppUser.MergeUsersIds(
                    result.Id,
                    result.ProjectTaskAppUsers,
                    assigneeUpdate.UserIds).ToList();
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

            var diff = ProjectTaskDiff.Create(old, response);
            mutationOutcome = await TaskMutationPipeline.Record(new TaskMutationRequest
            {
                Previous = old,
                Current = response,
                Diff = diff,
                ActorUserId = Identity.GetCurrentUserId(),
            }, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);
        });

        if (response is null)
        {
            return ClientResponse<TaskViewModel>.NotFound;
        }

        if (mutationOutcome is not null)
        {
            await TaskMutationPipeline.Publish(mutationOutcome);
        }

        return ClientResponse<TaskViewModel>.Success(response);
    }
}
