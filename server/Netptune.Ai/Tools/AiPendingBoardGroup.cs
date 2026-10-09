using Mediator;

using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

internal sealed record AiPendingBoardGroup
{
    public required string RefKey { get; init; }

    public required string Name { get; init; }

    public required string BoardName { get; init; }

    public int? ProjectId { get; init; }

    public string? ProjectRef { get; init; }

    public string Label => $"{BoardName} · {Name}";
}

internal sealed record AiPendingBoardGroupResult(AiPendingBoardGroup? Group, string? Error)
{
    public static AiPendingBoardGroupResult Found(AiPendingBoardGroup group)
    {
        return new AiPendingBoardGroupResult(group, null);
    }

    public static AiPendingBoardGroupResult Failed(string error)
    {
        return new AiPendingBoardGroupResult(null, error);
    }
}

internal static class AiPendingBoardGroupLookup
{
    public const string EntityType = "boardGroup";

    // The group's project decides which tasks it can hold, and it is only known through its board: an
    // existing board is looked up, a pending one carries its project in its own payload.
    public static async Task<AiPendingBoardGroupResult> Find(
        IMediator mediator,
        IAiChangeSetBuilder changeSet,
        string refKey,
        CancellationToken cancellationToken)
    {
        var pending = AiPendingReference.Find(changeSet, refKey, EntityType);

        if (pending is null)
        {
            return AiPendingBoardGroupResult.Failed(AiPendingReference.Missing(refKey, "board group"));
        }

        var payload = pending.Payload.RootElement;
        var board = pending.Fields.FirstOrDefault(field => string.Equals(field.Name, "board", StringComparison.Ordinal));
        var group = new AiPendingBoardGroup
        {
            RefKey = refKey,
            Name = AiPendingReference.ProposedName(pending),
            BoardName = board?.After ?? string.Empty,
        };

        var boardRef = AiPendingReference.Read(payload, "boardRef");

        if (boardRef is not null)
        {
            var pendingBoard = AiPendingReference.Find(changeSet, boardRef, "board");
            var boardPayload = pendingBoard?.Payload.RootElement;

            return AiPendingBoardGroupResult.Found(group with
            {
                ProjectId = boardPayload is null ? null : AiToolSchema.GetInt(boardPayload.Value, "projectId"),
                ProjectRef = boardPayload is null ? null : AiPendingReference.Read(boardPayload.Value, "projectRef"),
            });
        }

        var boardId = AiToolSchema.GetInt(payload, "boardId");
        var existingBoard = boardId.HasValue
            ? await AiBoardLookup.FindBoard(mediator, boardId.Value, cancellationToken)
            : null;

        if (existingBoard is null)
        {
            return AiPendingBoardGroupResult.Failed($"The board for group “{group.Name}” is not in this workspace.");
        }

        return AiPendingBoardGroupResult.Found(group with { ProjectId = existingBoard.ProjectId });
    }

    public static string? FindProjectMismatch(AiPendingBoardGroup group, int? projectId, string? projectRef)
    {
        var isSameProject = projectRef is not null
            ? string.Equals(group.ProjectRef, projectRef, StringComparison.Ordinal)
            : projectId.HasValue && group.ProjectId == projectId;

        if (isSameProject)
        {
            return null;
        }

        return $"Board group “{group.Name}” ({group.RefKey}) is on {group.BoardName}, which belongs to a different project.";
    }
}
