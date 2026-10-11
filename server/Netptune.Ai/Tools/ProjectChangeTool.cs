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
        });
    }

    public string Name => ToolName;

    public string Description =>
        "Propose creating a project, or changing a project's name, description or repository url. "
        + "On update, fields that are not passed keep their current value.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions => Actions.AllPermissions;

    public IReadOnlyList<string> ProposedChanges => Actions.ProposedChanges;

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "action": { "type": "string", "enum": ["create", "update"] },
          "projectId": { "type": "integer", "description": "Update: the project, from list_records." },
          "name": { "type": "string", "description": "Create: the project name. Update: a new name." },
          "description": { "type": "string", "description": "Optional project description." },
          "repositoryUrl": { "type": "string", "description": "Optional source repository url." }
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
