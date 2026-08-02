using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.Ai.Tools;

public sealed class DeleteTaskTool : IAiTool
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public DeleteTaskTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_delete_task";

    public string Description =>
        "Propose deleting a task — for duplicates, mistakes and abandoned work. "
        + "The task is archived rather than erased, so it can be restored afterwards. "
        + "Only tasks can be deleted this way; projects and boards cannot. "
        + "Nothing is deleted until the user reviews and applies the change.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Tasks.Delete };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "taskId": { "type": "integer", "description": "The id of the task to delete." },
          "reason": { "type": "string", "description": "Why the task should go, shown to the user in the review." }
        }
        """,
        "taskId");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var taskId = AiToolSchema.GetInt(arguments, "taskId");

        if (!taskId.HasValue)
        {
            return AiToolExecution.Failed("A taskId is required.");
        }

        var task = await Mediator.Send(new GetTaskQuery(taskId.Value), cancellationToken);

        if (task is null)
        {
            return AiToolExecution.Failed($"Task {taskId} was not found in this workspace.");
        }

        var fields = new List<AiChangeField>
        {
            new() { Name = "task", Before = $"{task.SystemId} · {task.Name}", After = null },
            new() { Name = "status", Before = task.StatusName },
        };

        AiToolSchema.AddOptionalField(fields, "reason", AiToolSchema.GetString(arguments, "reason"));

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "task",
            EntityId = task.Id,
            Summary = $"Delete “{task.Name}” ({task.SystemId})",
            Fields = fields,
            Payload = JsonSerializer.SerializeToDocument(new { taskId = task.Id }),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed deleting {task.SystemId}. Nothing has been deleted yet — the user must review and apply the change.");
    }
}
