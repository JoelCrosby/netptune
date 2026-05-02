using Mediator;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Meta;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Services.Boards.Commands;

public sealed record CreateBoardCommand(AddBoardRequest Request) : IRequest<ClientResponse<BoardViewModel>>;

public sealed class CreateBoardCommandHandler : IRequestHandler<CreateBoardCommand, ClientResponse<BoardViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IActivityLogger Activity;

    public CreateBoardCommandHandler(INetptuneUnitOfWork unitOfWork, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<BoardViewModel>> Handle(CreateBoardCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        if (!req.ProjectId.HasValue)
        {
            throw new Exception($"{nameof(req.ProjectId)} is required");
        }

        var project = await UnitOfWork.Projects.GetAsync(req.ProjectId.Value, true, cancellationToken);

        if (project is null) return ClientResponse<BoardViewModel>.NotFound;

        var workspaceId = project.WorkspaceId;

        var board = new Board
        {
            Name = req.Name,
            Identifier = req.Identifier.ToUrlSlug(),
            ProjectId = req.ProjectId.Value,
            MetaInfo = req.Meta ?? new BoardMeta(),
            WorkspaceId = workspaceId,
        };

        board.BoardGroups.Add(new() { Name = "Backlog", Type = BoardGroupType.Backlog, SortOrder = 1D, WorkspaceId = workspaceId });
        board.BoardGroups.Add(new() { Name = "Todo", Type = BoardGroupType.Todo, SortOrder = 1.1D, WorkspaceId = workspaceId });
        board.BoardGroups.Add(new() { Name = "Done", Type = BoardGroupType.Done, SortOrder = 1.2D, WorkspaceId = workspaceId });

        var result = await UnitOfWork.Boards.AddAsync(board, cancellationToken);

        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = result.Id;
            options.EntityType = EntityType.Board;
            options.Type = ActivityType.Create;
        });

        return result.ToViewModel();
    }
}
