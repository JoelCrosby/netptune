using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.Ai.Tools;

public sealed class RemoveTaskFromSprintTool : IAiTool
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public RemoveTaskFromSprintTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_remove_task_from_sprint";

    public string Description =>
        "Propose taking a task out of the sprint it is in, sending it back to the backlog. "
        + "The task is not moved until the user reviews and applies the change.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Sprints.ManageTasks };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "taskId": { "type": "integer", "description": "The id of the task to take out of its sprint." }
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

        if (!task.SprintId.HasValue)
        {
            return AiToolExecution.Failed($"Task {taskId} is not in a sprint.");
        }

        var payload = new
        {
            taskId = task.Id,
            sprintId = task.SprintId.Value,
        };

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "task",
            EntityId = task.Id,
            Summary = $"Take “{task.Name}” out of {task.SprintName}",
            Fields =
            [
                AiChangeFields.Values(
                    "sprint",
                    AiChangeValueKind.Sprint,
                    [AiChangeFields.Sprint(task.SprintId, task.SprintName!)],
                    []),
            ],
            Payload = JsonSerializer.SerializeToDocument(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed removing task {task.Id} from {task.SprintName}. Nothing has been applied yet — the user must review and apply the change.");
    }
}
