using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Statuses.Commands;

public sealed record ReorderStatusesCommand(ReorderStatusesRequest Request) : IRequest<ClientResponse>;

public sealed class ReorderStatusesCommandHandler : IRequestHandler<ReorderStatusesCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public ReorderStatusesCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(ReorderStatusesCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null) return ClientResponse.NotFound;

        var statuses = await UnitOfWork.Statuses.GetAllInWorkspace(workspaceId.Value, isReadonly: false, cancellationToken: cancellationToken);
        var statusMap = statuses
            .Where(status => status.EntityType == request.Request.EntityType)
            .ToDictionary(status => status.Id);

        foreach (var (statusId, index) in request.Request.StatusIds.Select((statusId, index) => (statusId, index)))
        {
            if (statusMap.TryGetValue(statusId, out var status))
            {
                status.SortOrder = index;
            }
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.LogMany(options =>
        {
            options.EntityIds = request.Request.StatusIds.Where(statusMap.ContainsKey);
            options.EntityType = EntityType.Status;
            options.Type = ActivityType.Reorder;
        });

        return ClientResponse.Success;
    }
}
