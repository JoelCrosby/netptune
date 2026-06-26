using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Tasks.Commands;

public sealed record DeleteTasksCommand(IEnumerable<int> Ids) : IRequest<ClientResponse>;

public sealed class DeleteTasksCommandHandler : IRequestHandler<DeleteTasksCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public DeleteTasksCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(DeleteTasksCommand request, CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();

        var deletedIds = await UnitOfWork.Tasks.SoftDelete(request.Ids, userId, cancellationToken);

        Activity.LogMany(options =>
        {
            options.EntityIds = deletedIds;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Delete;
        });

        return ClientResponse.Success;
    }
}
