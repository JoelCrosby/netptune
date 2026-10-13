using System.ComponentModel.DataAnnotations;

using Netptune.Core.Requests;

namespace Netptune.Api.Requests;

public sealed record PublicMoveTasksRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(RequestLimits.MaxBulkIds)]
    public List<string> SystemIds { get; init; } = [];

    [Required]
    public int BoardGroupId { get; init; }

    public int? Position { get; init; }

    internal MoveTaskInGroupRequest ToMoveInGroupRequest(int taskId, int currentBoardGroupId)
    {
        return new MoveTaskInGroupRequest
        {
            TaskId = taskId,
            NewGroupId = BoardGroupId,
            OldGroupId = currentBoardGroupId,
            CurrentIndex = Position!.Value,
        };
    }
}
