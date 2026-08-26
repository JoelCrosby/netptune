using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.BoardGroups.Commands;

public sealed record DeleteBoardGroupCommand(int Id) : IRequest<ClientResponse>;

public sealed class DeleteBoardGroupCommandHandler : IRequestHandler<DeleteBoardGroupCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public DeleteBoardGroupCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(DeleteBoardGroupCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var boardGroup = await UnitOfWork.BoardGroups.GetInWorkspace(request.Id, workspaceId, cancellationToken: cancellationToken);

        if (boardGroup is null) return ClientResponse.NotFound;

        var userId = Identity.GetCurrentUserId();

        boardGroup.Delete(userId);

        await RelocatePlacements(boardGroup.Id, boardGroup.BoardId, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = boardGroup.Id;
            options.EntityType = EntityType.BoardGroup;
            options.Type = ActivityType.Delete;
        });

        return ClientResponse.Success;
    }

    private async Task RelocatePlacements(int groupId, int boardId, CancellationToken cancellationToken)
    {
        var fallback = await UnitOfWork.BoardGroups.GetFallbackTaskTarget(boardId, groupId, cancellationToken);

        if (fallback is null)
        {
            await UnitOfWork.ProjectTasksInGroups.DeletePlacementsInGroup(groupId, cancellationToken);

            return;
        }

        var baseSortOrder = fallback.MaxSortOrder + 1;

        await UnitOfWork.ProjectTasksInGroups.MovePlacementsToGroup(groupId, fallback.Id, baseSortOrder, cancellationToken);
    }
}
