using System.Text.Json;

using Mediator;

using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class RelationTypeChangeTool : IAiTool
{
    public const string ToolName = "propose_relation_type_change";

    private readonly AiToolActions Actions;

    public RelationTypeChangeTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Actions = new AiToolActions(new Dictionary<string, IAiToolAction>(StringComparer.Ordinal)
        {
            ["create"] = new CreateRelationTypeAction(mediator, changeSet),
            ["update"] = new UpdateRelationTypeAction(mediator, changeSet),
            ["delete"] = new DeleteRelationTypeAction(mediator, changeSet),
        });
    }

    public string Name => ToolName;

    public string Description =>
        "Propose creating, updating or deleting a relation type tasks can be linked with. "
        + "Create only for a relationship no existing type covers — check the relation_types in list_records first. "
        + "Once created in a change set, propose_task_link_change may use it through the handle it answers with. "
        + "A category cannot be changed after creation, and system types or types still in use cannot be deleted.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions => Actions.AllPermissions;

    public IReadOnlyList<string> ProposedChanges => Actions.ProposedChanges;

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "action": { "type": "string", "enum": ["create", "update", "delete"] },
          "relationTypeId": { "type": "integer", "description": "Update and delete: the relation type, from list_records." },
          "name": { "type": "string", "description": "Create and update: what the link is called read from the first task, for example \"Blocks\"." },
          "inverseName": { "type": "string", "description": "Create and update: what it is called read from the other task, for example \"Is Blocked By\". A related type reads the same both ways and ignores this." },
          "category": {
            "type": "string",
            "description": "Create: how the link behaves. Hierarchy gives each task one parent and forbids cycles, dependency forbids cycles, related reads the same both ways, duplicate marks the same work twice over.",
            "enum": ["Hierarchy", "Dependency", "Related", "Duplicate"]
          },
          "description": { "type": "string", "description": "Create and update: what the link means." },
          "color": { "type": "string", "description": "Create and update: a colour, as a hex value or a named colour." },
          "reason": { "type": "string", "description": "Delete: why the type should go, shown in the review." }
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
