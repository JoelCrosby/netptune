using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Tasks.Commands;

public sealed record DeleteTaskCommand(int Id) : IRequest<ClientResponse>;

public sealed class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public DeleteTaskCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();

        var affected = await UnitOfWork.Tasks.SoftDelete(request.Id, userId, cancellationToken);

        if (affected == 0) return ClientResponse.NotFound;

        Activity.Log(options =>
        {
            options.EntityId = request.Id;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Delete;
        });

        return ClientResponse.Success;
    }
}
