using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Models.Search;
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
    private readonly IEventPublisher EventPublisher;

    public DeleteTasksCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity, IEventPublisher eventPublisher)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
        EventPublisher = eventPublisher;
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

        var workspaceSlug = Identity.GetWorkspaceKey();

        foreach (var id in deletedIds)
        {
            await EventPublisher.Dispatch(new SearchIndexEvent
            {
                Operation = SearchIndexOperation.Delete,
                EntityType = "task",
                EntityId = id,
                WorkspaceSlug = workspaceSlug,
            });
        }

        return ClientResponse.Success;
    }
}
