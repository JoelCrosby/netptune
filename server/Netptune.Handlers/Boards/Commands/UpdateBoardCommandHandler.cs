using Mediator;
using Netptune.Core.Encoding;
using Netptune.Core.Enums;
using Netptune.Core.Meta;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Handlers.Boards.Commands;

public sealed record UpdateBoardCommand(UpdateBoardRequest Request) : IRequest<ClientResponse<BoardViewModel>>;

public sealed class UpdateBoardCommandHandler : IRequestHandler<UpdateBoardCommand, ClientResponse<BoardViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public UpdateBoardCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<BoardViewModel>> Handle(UpdateBoardCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        if (!req.Id.HasValue) throw new Exception($"{nameof(req.Id)} is required");

        var workspaceId = await Identity.GetWorkspaceId();
        var result = await UnitOfWork.Boards.GetInWorkspace(req.Id.Value, workspaceId, cancellationToken: cancellationToken);

        if (result is null) return ClientResponse<BoardViewModel>.NotFound;

        result.Name = req.Name ?? result.Name;
        result.Identifier = req.Identifier?.ToUrlSlug() ?? result.Identifier;
        result.MetaInfo = MetaMerge.Apply(result.MetaInfo, req.Meta);

        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = result.Id;
            options.EntityType = EntityType.Board;
            options.Type = ActivityType.Modify;
        });

        return result.ToViewModel();
    }
}
