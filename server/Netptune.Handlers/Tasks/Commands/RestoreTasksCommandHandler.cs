using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Models.Search;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Tasks.Commands;

public sealed record RestoreTasksCommand(IEnumerable<int> Ids) : IRequest<ClientResponse>;

public sealed class RestoreTasksCommandHandler : IRequestHandler<RestoreTasksCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;
    private readonly IEventPublisher EventPublisher;

    public RestoreTasksCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity, IEventPublisher eventPublisher)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
        EventPublisher = eventPublisher;
    }

    public async ValueTask<ClientResponse> Handle(RestoreTasksCommand request, CancellationToken cancellationToken)
    {
        var workspaceSlug = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceSlug, cancellationToken);

        if (!workspaceId.HasValue)
        {
            return ClientResponse.Failed($"workspace with key {workspaceSlug} not found");
        }

        var deletedIds = await UnitOfWork.Tasks.GetDeletedTaskIdsInWorkspace(request.Ids, workspaceId.Value, cancellationToken);
        var restoredIds = await UnitOfWork.Tasks.Restore(deletedIds, cancellationToken);

        if (restoredIds.Count == 0) return ClientResponse.Success;

        Activity.LogMany(options =>
        {
            options.EntityIds = restoredIds;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Restore;
        });

        foreach (var id in restoredIds)
        {
            await EventPublisher.Dispatch(new SearchIndexEvent
            {
                Operation = SearchIndexOperation.Index,
                EntityType = "task",
                EntityId = id,
                WorkspaceSlug = workspaceSlug,
            });
        }

        return ClientResponse.Success;
    }
}
