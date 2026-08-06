using System.Text.Json;

namespace Netptune.Transfer.Undo;

public sealed record EntityUndoContext
{
    public required int WorkspaceId { get; init; }

    public required string UserId { get; init; }

    public required int EntityId { get; init; }

    public JsonDocument? PreviousValues { get; init; }

    public DateTime? ExpectedUpdatedAt { get; init; }
}

public sealed record EntityUndoResult
{
    public required bool IsSuccess { get; init; }

    public string? Reason { get; init; }

    public static EntityUndoResult Success { get; } = new() { IsSuccess = true };

    public static EntityUndoResult Blocked(string reason)
    {
        return new EntityUndoResult { IsSuccess = false, Reason = reason };
    }
}

public interface IEntityUndoHandler
{
    string EntityType { get; }

    Task<EntityUndoResult> RevertCreate(EntityUndoContext context, CancellationToken cancellationToken = default);

    Task<EntityUndoResult> RevertUpdate(EntityUndoContext context, CancellationToken cancellationToken = default);
}
