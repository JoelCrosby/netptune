using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Models.Search;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Sprints.Commands;

public sealed record DeleteSprintCommand(int Id) : IRequest<ClientResponse>;

public sealed class DeleteSprintCommandHandler : IRequestHandler<DeleteSprintCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;
    private readonly IEventPublisher EventPublisher;

    public DeleteSprintCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity, IEventPublisher eventPublisher)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
        EventPublisher = eventPublisher;
    }

    public async ValueTask<ClientResponse> Handle(DeleteSprintCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var sprint = await UnitOfWork.Sprints.GetSprintInWorkspaceAsync(workspaceKey, request.Id, cancellationToken: cancellationToken);

        if (sprint is null) return ClientResponse.NotFound;
        if (sprint.Status is SprintStatus.Active or SprintStatus.Completed)
        {
            return ClientResponse.Failed("Only planning or cancelled sprints can be deleted");
        }

        var user = await Identity.GetCurrentUser();

        foreach (var task in sprint.ProjectTasks)
        {
            task.SprintId = null;
        }

        sprint.Delete(user.Id);

        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = sprint.Id;
            options.EntityType = EntityType.Sprint;
            options.Type = ActivityType.Delete;
        });

        await EventPublisher.Dispatch(new SearchIndexEvent
        {
            Operation = SearchIndexOperation.Delete,
            EntityType = "sprint",
            EntityId = sprint.Id,
            WorkspaceSlug = workspaceKey,
        });

        return ClientResponse.Success;
    }
}
