using System.Text.Json;

using Mediator;

using Netptune.Core.Services;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class CommentChangeTool : IAiTool
{
    public const string ToolName = "propose_comment_change";

    private readonly AiToolActions Actions;

    public CommentChangeTool(IMediator mediator, IAiChangeSetBuilder changeSet, IIdentityService identity)
    {
        Actions = new AiToolActions(new Dictionary<string, IAiToolAction>(StringComparer.Ordinal)
        {
            ["add"] = new AddCommentAction(mediator, changeSet),
            ["update"] = new UpdateCommentAction(mediator, changeSet, identity),
            ["delete"] = new DeleteCommentAction(mediator, changeSet, identity),
        });
    }

    public string Name => ToolName;

    public string Description =>
        "Propose adding a comment to a task, editing one of the user's own comments, or deleting a comment. "
        + "Comments are posted under the user's own name. "
        + "Read comment ids, and which comments are the user's own, from get_task with include comments.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions => Actions.AllPermissions;

    public IReadOnlyList<string> ProposedChanges => Actions.ProposedChanges;

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "action": { "type": "string", "enum": ["add", "update", "delete"] },
          "taskId": { "type": "integer", "description": "Add: the id of the task to comment on." },
          "taskRef": { "type": "string", "description": "Add: handle of a task proposed earlier in this change set, instead of taskId." },
          "systemId": { "type": "string", "description": "Update and delete: the task's system id, for example NPT-42." },
          "commentId": { "type": "integer", "description": "Update and delete: the comment, from get_task with include comments." },
          "comment": { "type": "string", "description": "Add and update: the comment body." },
          "reason": { "type": "string", "description": "Delete: why the comment should go, shown in the review." }
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
