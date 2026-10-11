using System.Text.Json;

using Mediator;

using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class TaskLinkChangeTool : IAiTool
{
    public const string ToolName = "propose_task_link_change";

    private readonly AiToolActions Actions;

    public TaskLinkChangeTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Actions = new AiToolActions(new Dictionary<string, IAiToolAction>(StringComparer.Ordinal)
        {
            ["link"] = new LinkTasksAction(mediator, changeSet),
            ["unlink"] = new UnlinkTasksAction(mediator, changeSet),
        });
    }

    public string Name => ToolName;

    public string Description =>
        "Propose linking two tasks with a relation such as blocks or relates to, or removing a relation between them. "
        + "To link, name the relation type with relationType — its ids differ between workspaces, so never guess one. "
        + "To unlink, read the relation id from get_task with include relations first.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions => Actions.AllPermissions;

    public IReadOnlyList<string> ProposedChanges => Actions.ProposedChanges;

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "action": { "type": "string", "enum": ["link", "unlink"] },
          "taskId": { "type": "integer", "description": "Link: the id of the task the relation is stated from." },
          "taskRef": { "type": "string", "description": "Link: handle of a task proposed earlier in this change set, instead of taskId." },
          "relatedTaskId": { "type": "integer", "description": "Link: the id of the task on the other end of the relation." },
          "relatedTaskRef": { "type": "string", "description": "Link: handle of a task proposed earlier in this change set, instead of relatedTaskId." },
          "relationType": { "type": "string", "description": "Link: the relation type by name, such as \"blocks\" or \"relates to\". Its inverse name, such as \"is blocked by\", states the relation the other way round and swaps the two tasks." },
          "relationTypeId": { "type": "integer", "description": "Link: the relation type id, from list_records, instead of relationType." },
          "relationTypeRef": { "type": "string", "description": "Link: handle of a relation type proposed earlier in this change set, instead of relationType." },
          "systemId": { "type": "string", "description": "Unlink: the task's system id, for example NPT-42." },
          "relationId": { "type": "integer", "description": "Unlink: the relation id, from get_task with include relations." }
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
