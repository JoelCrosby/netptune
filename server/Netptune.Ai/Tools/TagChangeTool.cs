using System.Text.Json;

using Mediator;

using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class TagChangeTool : IAiTool
{
    public const string ToolName = "propose_tag_change";

    private readonly AiToolActions Actions;

    public TagChangeTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Actions = new AiToolActions(new Dictionary<string, IAiToolAction>(StringComparer.Ordinal)
        {
            ["create"] = new CreateTagAction(mediator, changeSet),
            ["update"] = new UpdateTagAction(mediator, changeSet),
            ["delete"] = new DeleteTagAction(mediator, changeSet),
        });
    }

    public string Name => ToolName;

    public string Description =>
        "Propose creating, renaming or deleting a workspace tag. "
        + "Create only for a theme no existing tag covers; once proposed, task and page changes in the same change set may use it. "
        + "A renamed tag stays on its tasks and pages, so other changes in the same change set still use its current name. "
        + "Deleting removes the tag from every task and page and cannot be undone.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions => Actions.AllPermissions;

    public IReadOnlyList<string> ProposedChanges => Actions.ProposedChanges;

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "action": { "type": "string", "enum": ["create", "update", "delete"] },
          "tagId": { "type": "integer", "description": "Update and delete: the tag, from list_records." },
          "name": { "type": "string", "description": "Create: the tag name. Update: the new name. 2 to 128 characters." },
          "reason": { "type": "string", "description": "Delete: why the tag should go, shown in the review." }
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
