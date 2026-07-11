using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Models.Search;
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
    private readonly IEventPublisher EventPublisher;

    public DeleteTaskCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity, IEventPublisher eventPublisher)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
        EventPublisher = eventPublisher;
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

        await EventPublisher.Dispatch(new SearchIndexEvent
        {
            Operation = SearchIndexOperation.Delete,
            EntityType = "task",
            EntityIds = [request.Id],
            WorkspaceSlug = Identity.GetWorkspaceKey(),
        });

        return ClientResponse.Success;
    }
}
