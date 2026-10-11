using System.Text.Json;

using Mediator;

using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class StatusChangeTool : IAiTool
{
    public const string ToolName = "propose_status_change";

    private readonly AiToolActions Actions;

    public StatusChangeTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Actions = new AiToolActions(new Dictionary<string, IAiToolAction>(StringComparer.Ordinal)
        {
            ["create"] = new CreateStatusAction(mediator, changeSet),
            ["update"] = new UpdateStatusAction(mediator, changeSet),
        });
    }

    public string Name => ToolName;

    public string Description =>
        "Propose creating a workspace task status, or renaming one or changing its category, colour or description. "
        + "On update, fields that are not passed keep their current value.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions => Actions.AllPermissions;

    public IReadOnlyList<string> ProposedChanges => Actions.ProposedChanges;

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "action": { "type": "string", "enum": ["create", "update"] },
          "statusId": { "type": "integer", "description": "Update: the status, from list_records." },
          "name": { "type": "string", "description": "Create: the status name. Update: a new name." },
          "category": {
            "type": "string",
            "description": "Create: which stage the status belongs to. Update: a new stage.",
            "enum": ["New", "Backlog", "Todo", "Active", "Done", "Inactive"]
          },
          "description": { "type": "string", "description": "Optional status description." },
          "color": { "type": "string", "description": "Optional colour, as a hex value or a named colour." }
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
        return Actions.GetChangePermissions(changeName, payload);
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
