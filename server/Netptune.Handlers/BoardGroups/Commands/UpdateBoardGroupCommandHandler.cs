using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Handlers.BoardGroups.Commands;

public sealed record UpdateBoardGroupCommand(UpdateBoardGroupRequest Request) : IRequest<ClientResponse<BoardGroupViewModel>>;

public sealed class UpdateBoardGroupCommandHandler : IRequestHandler<UpdateBoardGroupCommand, ClientResponse<BoardGroupViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IActivityLogger Activity;

    public UpdateBoardGroupCommandHandler(INetptuneUnitOfWork unitOfWork, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<BoardGroupViewModel>> Handle(UpdateBoardGroupCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var result = await UnitOfWork.BoardGroups.GetAsync(req.BoardGroupId!.Value, cancellationToken: cancellationToken);

        if (result is null) return ClientResponse<BoardGroupViewModel>.NotFound;

        result.Name = req.Name ?? result.Name;
        result.SortOrder = req.SortOrder ?? result.SortOrder;

        if (req.ClearStatus)
        {
            result.StatusId = null;
        }
        else if (req.StatusId.HasValue)
        {
            var status = await UnitOfWork.Statuses.GetInWorkspace(req.StatusId.Value, result.WorkspaceId, cancellationToken: cancellationToken);

            if (status is not { EntityType: EntityType.Task })
            {
                return ClientResponse<BoardGroupViewModel>.Failed("Assigned status was not found or is not a task status.");
            }

            result.StatusId = req.StatusId;
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = result.Id;
            options.EntityType = EntityType.BoardGroup;
            options.Type = ActivityType.Modify;
        });

        return result.ToViewModel();
    }
}
