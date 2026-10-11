using System.Text.Json;

using Mediator;

using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class ProjectChangeTool : IAiTool
{
    public const string ToolName = "propose_project_change";

    private readonly AiToolActions Actions;

    public ProjectChangeTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Actions = new AiToolActions(new Dictionary<string, IAiToolAction>(StringComparer.Ordinal)
        {
            ["create"] = new CreateProjectAction(mediator, changeSet),
            ["update"] = new UpdateProjectAction(mediator, changeSet),
            ["delete"] = new DeleteProjectAction(mediator, changeSet),
        });
    }

    public string Name => ToolName;

    public string Description =>
        "Propose creating, updating or deleting a project. "
        + "On update, fields that are not passed keep their current value. "
        + "Deleting a project cannot be undone from the assistant.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions => Actions.AllPermissions;

    public IReadOnlyList<string> ProposedChanges => Actions.ProposedChanges;

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "action": { "type": "string", "enum": ["create", "update", "delete"] },
          "projectId": { "type": "integer", "description": "Update and delete: the project, from list_records." },
          "name": { "type": "string", "description": "Create: the project name. Update: a new name." },
          "description": { "type": "string", "description": "Optional project description." },
          "repositoryUrl": { "type": "string", "description": "Optional source repository url." },
          "reason": { "type": "string", "description": "Delete: why the project should go, shown in the review." }
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
