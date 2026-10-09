using System.Text.Json;

using Mediator;

using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class BoardChangeTool : IAiTool
{
    private readonly AiToolActions Actions;

    public BoardChangeTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Actions = new AiToolActions(new Dictionary<string, IAiToolAction>(StringComparer.Ordinal)
        {
            ["create"] = new CreateBoardAction(mediator, changeSet),
            ["update"] = new UpdateBoardAction(mediator, changeSet),
            ["delete"] = new DeleteBoardAction(mediator, changeSet),
        });
    }

    public string Name => "propose_board_change";

    public string Description =>
        "Propose creating, updating or deleting a board. "
        + "Deleting archives the board and its groups; its tasks stay in the project.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions => Actions.AllPermissions;

    public IReadOnlyList<string> ProposedChanges => Actions.ProposedChanges;

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "action": { "type": "string", "enum": ["create", "update", "delete"] },
          "boardId": { "type": "integer", "description": "Update and delete: the board, from list_records." },
          "name": { "type": "string", "description": "Create: the board name. Update: a new name." },
          "identifier": {
            "type": "string",
            "description": "Create: url identifier, lowercase with dashes. Update: a new one; otherwise the current one stays, so links keep working."
          },
          "projectId": { "type": "integer", "description": "Create: the project, from list_records." },
          "projectRef": { "type": "string", "description": "Create: handle of a project proposed earlier in this change set, instead of projectId." },
          "reason": { "type": "string", "description": "Delete: why the board should go, shown in the review." }
        }
        """,
        "action");

    public bool IsAvailable(IReadOnlySet<string> permissions)
    {
        return Actions.IsAvailable(permissions);
    }

    public IReadOnlySet<string> GetRequiredPermissions(JsonElement arguments)
    {
        return Actions.GetRequiredPermissions(arguments);
    }

    public IReadOnlySet<string> GetChangePermissions(string changeName, JsonElement payload)
    {
        return Actions.GetChangePermissions(changeName);
    }

    public string DescribeCall(JsonElement arguments)
    {
        return Actions.DescribeCall(Name, arguments);
    }

    public Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        return Actions.Execute(arguments, cancellationToken);
    }
}
