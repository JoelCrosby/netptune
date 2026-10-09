using System.Text.Json;

using Mediator;

using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class BoardGroupChangeTool : IAiTool
{
    private readonly AiToolActions Actions;

    public BoardGroupChangeTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Actions = new AiToolActions(new Dictionary<string, IAiToolAction>(StringComparer.Ordinal)
        {
            ["create"] = new CreateBoardGroupAction(mediator, changeSet),
            ["update"] = new UpdateBoardGroupAction(mediator, changeSet),
            ["delete"] = new DeleteBoardGroupAction(mediator, changeSet),
            ["reorder"] = new ReorderBoardGroupsAction(mediator, changeSet),
        });
    }

    public string Name => "propose_board_group_change";

    public string Description =>
        "Propose a change to the groups, the columns, on a board. "
        + "create adds one to an existing board or one proposed in this change set. "
        + "update renames a group or changes the status tasks take when moved into it. "
        + "delete moves the group's tasks to the first remaining group; a board keeps its last group, so delete the board instead. "
        + "reorder sets the order left to right.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions => Actions.AllPermissions;

    public IReadOnlyList<string> ProposedChanges => Actions.ProposedChanges;

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "action": { "type": "string", "enum": ["create", "update", "delete", "reorder"] },
          "boardGroupId": { "type": "integer", "description": "Update and delete: the group, from list_records." },
          "boardId": { "type": "integer", "description": "Create and reorder: the board, from list_records." },
          "boardRef": { "type": "string", "description": "Create: handle of a board proposed earlier in this change set, instead of boardId." },
          "name": { "type": "string", "description": "Create: the group name, such as In progress. Update: a new name." },
          "statusId": { "type": "integer", "description": "Create and update: the status tasks take when moved into the group." },
          "clearStatus": { "type": "boolean", "description": "Update: remove the group's status instead of setting one." },
          "groupIds": {
            "type": "array",
            "items": { "type": "integer" },
            "description": "Reorder: every group id on the board once, left to right. Read them in their current order with list_records first."
          },
          "reason": { "type": "string", "description": "Delete: why the group should go, shown in the review." }
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
