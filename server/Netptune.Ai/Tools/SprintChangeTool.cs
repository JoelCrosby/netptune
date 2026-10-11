using System.Text.Json;

using Mediator;

using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class SprintChangeTool : IAiTool
{
    public const string ToolName = "propose_sprint_change";

    private readonly AiToolActions Actions;

    public SprintChangeTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Actions = new AiToolActions(new Dictionary<string, IAiToolAction>(StringComparer.Ordinal)
        {
            ["create"] = new CreateSprintAction(mediator, changeSet),
            ["update"] = new UpdateSprintAction(mediator, changeSet),
            [SprintTransitionAction.Start] = new SprintTransitionAction(mediator, changeSet, SprintTransitionAction.Start),
            [SprintTransitionAction.Complete] = new SprintTransitionAction(mediator, changeSet, SprintTransitionAction.Complete),
            [SprintTransitionAction.Cancel] = new SprintTransitionAction(mediator, changeSet, SprintTransitionAction.Cancel),
            [SprintTransitionAction.Delete] = new SprintTransitionAction(mediator, changeSet, SprintTransitionAction.Delete),
        });
    }

    public string Name => ToolName;

    public string Description =>
        "Propose creating a sprint, changing its name, goal or dates, or moving it through its lifecycle. "
        + "start makes a planning sprint the project's active sprint; a project has one active sprint, "
        + "so complete or cancel the running one first. "
        + "complete closes the active sprint and sends its unfinished tasks back to the backlog; it can no longer be edited. "
        + "cancel sends its unfinished tasks back to the backlog and lets it be deleted afterwards. "
        + "delete removes a planning or cancelled sprint and sends its tasks back to the backlog.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions => Actions.AllPermissions;

    public IReadOnlyList<string> ProposedChanges => Actions.ProposedChanges;

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "action": { "type": "string", "enum": ["create", "update", "start", "complete", "cancel", "delete"] },
          "sprintId": { "type": "integer", "description": "Every action but create: the sprint, from list_records." },
          "name": { "type": "string", "description": "Create: the sprint name. Update: a new name." },
          "projectId": { "type": "integer", "description": "Create: the project the sprint belongs to." },
          "projectRef": { "type": "string", "description": "Create: handle of a project proposed earlier in this change set, instead of projectId." },
          "startDate": { "type": "string", "description": "Create and update: first day of the sprint as YYYY-MM-DD." },
          "endDate": { "type": "string", "description": "Create and update: last day of the sprint as YYYY-MM-DD." },
          "goal": { "type": "string", "description": "Create and update: the sprint goal." }
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
