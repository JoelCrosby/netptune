using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.ViewModels.Boards;
using Netptune.Core.ViewModels.Statuses;
using Netptune.Handlers.BoardGroups.Queries;
using Netptune.Handlers.Boards.Queries;
using Netptune.Handlers.Statuses.Queries;

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

    // A group with a status of its own gives that status to every task placed in it, whatever the task
    // asked for.
    public static async Task<StatusViewModel?> FindGroupStatus(
        IMediator mediator,
        int boardGroupId,
        AiTaskLookups? lookups,
        CancellationToken cancellationToken)
    {
        var lookupsKnow = lookups is not null && lookups.GroupStatusIds.ContainsKey(boardGroupId);
        var statusId = lookupsKnow ? lookups!.GroupStatusIds[boardGroupId] : null;

        if (!lookupsKnow)
        {
            var group = await mediator.Send(new GetBoardGroupQuery(boardGroupId), cancellationToken);

            statusId = group?.StatusId;
            lookups?.GroupStatusIds.Add(boardGroupId, statusId);
        }

        if (statusId is null)
        {
            return null;
        }

        var statuses = lookups?.Statuses
            ?? await mediator.Send(new GetStatusesQuery(new StatusFilter()), cancellationToken)
            ?? [];

        if (lookups is not null)
        {
            lookups.Statuses = statuses;
        }

        return statuses.FirstOrDefault(status => status.Id == statusId);
    }

    public static async Task<BoardGroupOptionViewModel?> FindGroup(
        IMediator mediator,
        int boardGroupId,
        AiTaskLookups? lookups,
        CancellationToken cancellationToken)
    {
        if (lookups?.BoardGroups is null)
        {
            return await FindGroup(mediator, boardGroupId, cancellationToken);
        }

        return lookups.BoardGroups.FirstOrDefault(option => option.Id == boardGroupId);
    }

    public static string StatusConflict(BoardGroupOptionViewModel group, StatusViewModel groupStatus)
    {
        return $"Board group “{group.Name}” gives its tasks the status “{groupStatus.Name}”, "
            + "so a different statusId would be overwritten. Leave statusId out, or pick a group without a status.";
    }

    public static string? FindProjectMismatch(BoardGroupOptionViewModel group, int? projectId)
    {
        var isSameProject = group.ProjectId == projectId;

        if (isSameProject)
        {
            return null;
        }

        return $"Board group “{group.Name}” is on {group.BoardName} in project {group.ProjectName}; "
            + "a task can only sit on boards in its own project.";
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
