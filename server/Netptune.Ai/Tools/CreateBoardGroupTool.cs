using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
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
        "Propose adding a group, the column on a board, to an existing board. "
        + "The group is not created until the user reviews and applies the change.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.BoardGroups.Create };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "name": { "type": "string", "description": "The group name, such as In progress." },
          "boardId": { "type": "integer", "description": "The board the group belongs to, from list_boards." },
          "statusId": { "type": "integer", "description": "Optional status tasks take when moved into this group." }
        }
        """,
        "name",
        "boardId");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var name = AiToolSchema.GetString(arguments, "name")?.Trim();
        var boardId = AiToolSchema.GetInt(arguments, "boardId");
        var hasName = !string.IsNullOrWhiteSpace(name);

        if (!hasName || !boardId.HasValue)
        {
            return AiToolExecution.Failed("A group name and boardId are required.");
        }

        var groups = await Mediator.Send(new GetBoardsInWorkspaceQuery(), cancellationToken);
        var board = groups?
            .SelectMany(group => group.Boards)
            .FirstOrDefault(item => item.Id == boardId.Value);

        if (board is null)
        {
            return AiToolExecution.Failed($"Board {boardId} is not in this workspace.");
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

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "board",
            EntityId = board.Id,
            Summary = $"Add group “{name}” to {board.Name}",
            Fields = fields,
            Payload = JsonDocument.Parse(arguments.GetRawText()),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed adding group \"{name}\" to {board.Name}. Nothing has been applied yet — the user must review and apply the change.");
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

        fields.Add(new AiChangeField { Name = "status", After = status.Name });

        return null;
    }
}
