using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class DeleteBoardTool : IAiTool
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public DeleteBoardTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_delete_board";

    public string Description =>
        "Propose deleting a board and its groups. "
        + "The board is archived rather than erased, and its tasks stay in the project.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Boards.Delete };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "boardId": { "type": "integer", "description": "The id of the board to delete, from list_boards." },
          "reason": { "type": "string", "description": "Why the board should go, shown to the user in the review." }
        }
        """,
        "boardId");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var boardId = AiToolSchema.GetInt(arguments, "boardId");

        if (!boardId.HasValue)
        {
            return AiToolExecution.Failed("A boardId is required.");
        }

        var board = await AiBoardLookup.FindBoard(Mediator, boardId.Value, cancellationToken);

        if (board is null)
        {
            return AiToolExecution.Failed($"Board {boardId} is not in this workspace.");
        }

        var fields = new List<AiChangeField>
        {
            AiChangeFields.Text("board", board.Name, null),
            AiChangeFields.Text("project", board.ProjectName, null),
        };

        var hasTasks = board.TaskCount > 0;

        if (hasTasks)
        {
            fields.Add(AiChangeFields.Text("tasks", $"{board.TaskCount}", null));
        }

        AiToolSchema.AddOptionalField(fields, "reason", AiToolSchema.GetString(arguments, "reason"));

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "board",
            EntityId = board.Id,
            Summary = $"Delete board “{board.Name}” in {board.ProjectName}",
            Fields = fields,
            Payload = JsonSerializer.SerializeToDocument(new { boardId = board.Id }),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed deleting board {board.Id}. "
            + "Nothing has been deleted yet — the user must review and apply the change.");
    }
}
