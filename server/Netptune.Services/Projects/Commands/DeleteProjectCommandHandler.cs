using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Projects.Commands;

public sealed record DeleteProjectCommand(int Id) : IRequest<ClientResponse>;

public sealed class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public DeleteProjectCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await UnitOfWork.Projects.GetAsync(request.Id);
        var userId = Identity.GetCurrentUserId();

        if (project is null) return ClientResponse.NotFound;

        project.Delete(userId);

        await UnitOfWork.CompleteAsync();

        Activity.Log(options =>
        {
            options.EntityId = project.Id;
            options.EntityType = EntityType.Project;
            options.Type = ActivityType.Delete;
        });

        return ClientResponse.Success;
    }
}
