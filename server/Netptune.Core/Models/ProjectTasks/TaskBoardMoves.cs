using Netptune.Core.Entities;
using Netptune.Core.Enums;

namespace Netptune.Core.Models.ProjectTasks;

public sealed record TaskBoardMove(int TaskId, int? FromGroupId, BoardGroupTaskTarget Target);

public sealed record TaskBoardMoveContext
{
    public required int WorkspaceId { get; init; }

    public required Guid CorrelationId { get; init; }

    public string? ActorUserId { get; init; }

    public Guid? CausationEventId { get; init; }

    public EventOriginType? OriginType { get; init; }
}

public sealed class TaskBoardMoves
{
    public static TaskBoardMoves None { get; } = new(null, []);

    public TaskBoardMoves(Status? causedByStatus, IReadOnlyList<TaskBoardMove> moves)
    {
        CausedByStatus = causedByStatus;
        Moves = moves;
    }

    public Status? CausedByStatus { get; }

    public IReadOnlyList<TaskBoardMove> Moves { get; }

    public bool HasMoves => Moves.Count > 0;
}
