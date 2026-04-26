using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Services.BoardGroups.Commands;

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
        var result = await UnitOfWork.BoardGroups.GetAsync(request.Request.BoardGroupId!.Value);

        if (result is null) return ClientResponse<BoardGroupViewModel>.NotFound;

        result.Name = request.Request.Name ?? result.Name;
        result.SortOrder = request.Request.SortOrder ?? result.SortOrder;

        await UnitOfWork.CompleteAsync();

        Activity.Log(options =>
        {
            options.EntityId = result.Id;
            options.EntityType = EntityType.BoardGroup;
            options.Type = ActivityType.Modify;
        });

        return result.ToViewModel();
    }
}
