using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Localization;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.BoardGroups.Commands;

public sealed record ReorderBoardGroupsCommand(int BoardId, List<int> GroupIds) : IRequest<ClientResponse>;

public sealed class ReorderBoardGroupsCommandHandler : IRequestHandler<ReorderBoardGroupsCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public ReorderBoardGroupsCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(
        ReorderBoardGroupsCommand request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var board = await UnitOfWork.Boards.GetInWorkspace(
            request.BoardId,
            workspaceId,
            cancellationToken: cancellationToken);

        if (board is null)
        {
            return ClientResponse.NotFound;
        }

        var groups = await UnitOfWork.BoardGroups.GetBoardGroupsInBoard(board.Id, cancellationToken: cancellationToken);
        var groupsById = groups.ToDictionary(group => group.Id);
        var listsEveryGroupOnce = request.GroupIds.Distinct().Count() == request.GroupIds.Count
            && request.GroupIds.Count == groupsById.Count
            && request.GroupIds.All(groupsById.ContainsKey);

        if (!listsEveryGroupOnce)
        {
            return ClientResponse.Failed(AppText.ErrorBoardGroupOrderMustListEveryGroup);
        }

        var position = 0;

        foreach (var groupId in request.GroupIds)
        {
            position++;
            groupsById[groupId].SortOrder = position;
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.LogMany(options =>
        {
            options.EntityIds = request.GroupIds;
            options.EntityType = EntityType.BoardGroup;
            options.Type = ActivityType.Reorder;
        });

        return ClientResponse.Success;
    }
}
