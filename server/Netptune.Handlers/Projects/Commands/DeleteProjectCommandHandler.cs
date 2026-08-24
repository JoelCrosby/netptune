using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Projects.Commands;

public sealed record DeleteProjectCommand(int Id) : IRequest<ClientResponse>;

public sealed class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ITaskPinRepository TaskPins;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public DeleteProjectCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        ITaskPinRepository taskPins,
        IIdentityService identity,
        IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        TaskPins = taskPins;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var project = await UnitOfWork.Projects.GetInWorkspace(request.Id, workspaceId, cancellationToken: cancellationToken);
        var userId = Identity.GetCurrentUserId();

        if (project is null) return ClientResponse.NotFound;

        project.Delete(userId);

        var pins = await TaskPins.GetForScopeEntity(workspaceId, TaskPinScope.Project, project.Id, cancellationToken);

        foreach (var pin in pins)
        {
            pin.Delete(userId);
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = project.Id;
            options.EntityType = EntityType.Project;
            options.Type = ActivityType.Delete;
        });

        return ClientResponse.Success;
    }
}
