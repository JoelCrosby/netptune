using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.Ai.Tools;

public sealed class AddTaskCommentTool : IAiTool
{
    private const int MaximumCommentLength = 4000;

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public AddTaskCommentTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_add_comment";

    public string Description =>
        "Propose adding a comment to a task. The comment is posted under the user's own name once they approve it.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Comments.Create };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "taskId": { "type": "integer", "description": "The id of the task to comment on." },
          "comment": { "type": "string", "description": "The comment body." }
        }
        """,
        "taskId",
        "comment");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var taskId = AiToolSchema.GetInt(arguments, "taskId");
        var comment = AiToolSchema.GetString(arguments, "comment")?.Trim();
        var hasComment = !string.IsNullOrWhiteSpace(comment);

        if (!taskId.HasValue || !hasComment)
        {
            return AiToolExecution.Failed("A taskId and comment are required.");
        }

        if (comment!.Length > MaximumCommentLength)
        {
            return AiToolExecution.Failed($"Comments must be {MaximumCommentLength} characters or fewer.");
        }

        var task = await Mediator.Send(new GetTaskQuery(taskId.Value), cancellationToken);

        if (task is null)
        {
            return AiToolExecution.Failed($"Task {taskId} was not found in this workspace.");
        }

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "task",
            EntityId = task.Id,
            Summary = $"Comment on “{task.Name}”",
            Fields = [new AiChangeField { Name = "comment", After = comment }],
            Payload = JsonDocument.Parse(arguments.GetRawText()),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed a comment on task {task.Id}. Nothing has been posted yet — the user must review and apply the change.");
    }
}
