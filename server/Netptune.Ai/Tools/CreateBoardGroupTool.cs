using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.BoardGroups.Queries;
using Netptune.Handlers.Boards.Queries;
using Netptune.Handlers.Statuses.Queries;

namespace Netptune.Ai.Tools;

public sealed class CreateBoardGroupTool : IAiTool
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public CreateBoardGroupTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_create_board_group";

    public string Description =>
        "Propose adding a group, the column on a board, to an existing board or one proposed in this change set.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.BoardGroups.Create };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "name": { "type": "string", "description": "The group name, such as In progress." },
          "boardId": { "type": "integer", "description": "The board the group belongs to, from list_boards." },
          "boardRef": { "type": "string", "description": "Handle of a board proposed earlier in this change set, instead of boardId." },
          "statusId": { "type": "integer", "description": "Optional status tasks take when moved into this group." }
        }
        """,
        "name");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var name = AiToolSchema.GetString(arguments, "name")?.Trim();
        var hasName = !string.IsNullOrWhiteSpace(name);

        if (!hasName)
        {
            return AiToolExecution.Failed("A group name is required.");
        }

        var parent = await ResolveBoard(arguments, cancellationToken);

        if (parent.Error is not null)
        {
            return AiToolExecution.Failed(parent.Error);
        }

        var board = parent.Parent!;
        var isDuplicate = await HasGroupNamed(name!, arguments, board, cancellationToken);

        if (isDuplicate)
        {
            return AiToolExecution.Failed($"{board.Name} already has a group called \"{name}\". Use that group instead.");
        }

        var fields = new List<AiChangeField>
        {
            new() { Name = "name", After = name },
            new() { Name = "board", After = board.Name },
        };

        var statusMessage = await AddStatusField(fields, arguments, cancellationToken);

        if (statusMessage is not null)
        {
            return AiToolExecution.Failed(statusMessage);
        }

        var refKey = ChangeSet.CreateRefKey();

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = AiPendingBoardGroupLookup.EntityType,
            RefKey = refKey,
            Summary = $"Add group “{name}” to {board.Name}",
            Fields = fields,
            Payload = JsonDocument.Parse(arguments.GetRawText()),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed adding group \"{name}\" to {board.Name} as {refKey}. "
            + "Nothing has been applied yet — the user must review and apply the change.");
    }

    private async Task<bool> HasGroupNamed(
        string name,
        JsonElement arguments,
        AiParent board,
        CancellationToken cancellationToken)
    {
        var boardRef = AiPendingReference.Read(arguments, "boardRef");
        var proposedNames = ChangeSet.Changes
            .Where(draft => draft.ToolName == Name && IsSameBoard(draft.Payload.RootElement, board.Id, boardRef))
            .Select(draft => AiToolSchema.GetString(draft.Payload.RootElement, "name")?.Trim());
        var existingNames = board.Id.HasValue
            ? await GetGroupNames(board.Id.Value, cancellationToken)
            : [];
        var takenNames = existingNames.Concat(proposedNames);

        return takenNames.Any(taken => string.Equals(taken, name, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSameBoard(JsonElement payload, int? boardId, string? boardRef)
    {
        if (boardRef is not null)
        {
            return string.Equals(AiPendingReference.Read(payload, "boardRef"), boardRef, StringComparison.Ordinal);
        }

        return boardId.HasValue && AiToolSchema.GetInt(payload, "boardId") == boardId;
    }

    private async Task<List<string?>> GetGroupNames(int boardId, CancellationToken cancellationToken)
    {
        var options = await Mediator.Send(new GetBoardGroupOptionsQuery(), cancellationToken);

        return options
            .Where(option => option.BoardId == boardId)
            .Select(option => (string?)option.Name)
            .ToList();
    }

    private async Task<AiParentResult> ResolveBoard(JsonElement arguments, CancellationToken cancellationToken)
    {
        var boardRef = AiPendingReference.Read(arguments, "boardRef");
        var boardId = AiToolSchema.GetInt(arguments, "boardId");
        var needsExistingBoard = boardRef is null && boardId.HasValue;

        if (!needsExistingBoard)
        {
            return AiParentLookup.Board(ChangeSet, arguments, null, null);
        }

        var board = await AiBoardLookup.FindBoard(Mediator, boardId!.Value, cancellationToken);

        if (board is null)
        {
            return AiParentResult.Failed($"Board {boardId} is not in this workspace.");
        }

        return AiParentLookup.Board(ChangeSet, arguments, board.Id, board.Name);
    }

    private async Task<string?> AddStatusField(
        List<AiChangeField> fields,
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var statusId = AiToolSchema.GetInt(arguments, "statusId");

        if (!statusId.HasValue)
        {
            return null;
        }

        var statuses = await Mediator.Send(new GetStatusesQuery(new StatusFilter()), cancellationToken);
        var status = statuses?.FirstOrDefault(item => item.Id == statusId.Value);

        if (status is null)
        {
            return $"Status {statusId} is not in this workspace.";
        }

        fields.Add(AiChangeFields.Values(
            "status",
            AiChangeValueKind.Status,
            [],
            [AiChangeFields.Status(status.Id, status.Name, status.Color)]));

        return null;
    }
}
