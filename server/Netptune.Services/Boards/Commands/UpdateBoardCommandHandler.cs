using Mediator;
using Netptune.Core.Encoding;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Services.Boards.Commands;

public sealed record UpdateBoardCommand(UpdateBoardRequest Request) : IRequest<ClientResponse<BoardViewModel>>;

public sealed class UpdateBoardCommandHandler : IRequestHandler<UpdateBoardCommand, ClientResponse<BoardViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IActivityLogger Activity;

    public UpdateBoardCommandHandler(INetptuneUnitOfWork unitOfWork, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<BoardViewModel>> Handle(UpdateBoardCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        if (!req.Id.HasValue) throw new Exception($"{nameof(req.Id)} is required");

        var result = await UnitOfWork.Boards.GetAsync(req.Id.Value);

        if (result is null) return ClientResponse<BoardViewModel>.NotFound;

        result.Name = req.Name ?? result.Name;
        result.Identifier = req.Identifier?.ToUrlSlug() ?? result.Identifier;

        await UnitOfWork.CompleteAsync();

        Activity.Log(options =>
        {
            options.EntityId = result.Id;
            options.EntityType = EntityType.Board;
            options.Type = ActivityType.Modify;
        });

        return result.ToViewModel();
    }
}
