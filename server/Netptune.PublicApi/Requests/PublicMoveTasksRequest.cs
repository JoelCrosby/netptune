using System.ComponentModel.DataAnnotations;

using Netptune.Core.Requests;

namespace Netptune.PublicApi.Requests;

public sealed record PublicMoveTasksRequest
{
    [Required]
    [MinLength(1)]
    public List<int> TaskIds { get; init; } = [];

    [Required]
    public int BoardGroupId { get; init; }

    public int? Position { get; init; }

    internal MoveTaskInGroupRequest ToMoveInGroupRequest(int currentBoardGroupId)
    {
        return new MoveTaskInGroupRequest
        {
            TaskId = TaskIds[0],
            NewGroupId = BoardGroupId,
            OldGroupId = currentBoardGroupId,
            CurrentIndex = Position!.Value,
        };
    }
}
