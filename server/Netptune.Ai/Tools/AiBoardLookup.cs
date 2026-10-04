using Mediator;

using Netptune.Core.ViewModels.Boards;
using Netptune.Handlers.BoardGroups.Queries;
using Netptune.Handlers.Boards.Queries;

namespace Netptune.Ai.Tools;

internal static class AiBoardLookup
{
    public static async Task<BoardViewModel?> FindBoard(
        IMediator mediator,
        int boardId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetBoardQuery(boardId), cancellationToken);

        return result.IsSuccess ? result.Payload : null;
    }

    public static async Task<BoardGroupOptionViewModel?> FindGroup(
        IMediator mediator,
        int boardGroupId,
        CancellationToken cancellationToken)
    {
        var options = await mediator.Send(new GetBoardGroupOptionsQuery(), cancellationToken);

        return options.FirstOrDefault(option => option.Id == boardGroupId);
    }

    public static async Task<List<BoardGroupOptionViewModel>> FindGroupsInBoard(
        IMediator mediator,
        int boardId,
        CancellationToken cancellationToken)
    {
        var options = await mediator.Send(new GetBoardGroupOptionsQuery(), cancellationToken);

        return options.Where(option => option.BoardId == boardId).ToList();
    }
}
