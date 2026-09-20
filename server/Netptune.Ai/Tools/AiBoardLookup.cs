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
        var projects = await mediator.Send(new GetBoardsInWorkspaceQuery(), cancellationToken);
        var boards = projects?.SelectMany(project => project.Boards);

        return boards?.FirstOrDefault(board => board.Id == boardId);
    }

    public static async Task<BoardGroupOptionViewModel?> FindGroup(
        IMediator mediator,
        int boardGroupId,
        CancellationToken cancellationToken)
    {
        var options = await mediator.Send(new GetBoardGroupOptionsQuery(), cancellationToken);

        return options.FirstOrDefault(option => option.Id == boardGroupId);
    }

    // The options query orders groups by sort order within a board, so this list is the column
    // order the user sees on the board.
    public static async Task<List<BoardGroupOptionViewModel>> FindGroupsInBoard(
        IMediator mediator,
        int boardId,
        CancellationToken cancellationToken)
    {
        var options = await mediator.Send(new GetBoardGroupOptionsQuery(), cancellationToken);

        return options.Where(option => option.BoardId == boardId).ToList();
    }
}
