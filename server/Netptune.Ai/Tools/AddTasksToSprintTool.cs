using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.Ai.Tools;

public sealed class AddTasksToSprintTool : IAiTool
{
    private const int MaximumTasks = 50;

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public AddTasksToSprintTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_add_tasks_to_sprint";

    public string Description =>
        "Propose adding several tasks to a sprint at once. The tasks must already belong to the sprint's project. "
        + "Nothing is moved until the user reviews and applies the change.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Sprints.ManageTasks };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "sprintId": { "type": "integer", "description": "The sprint to add the tasks to." },
          "taskIds": {
            "type": "array",
            "items": { "type": "integer" },
            "description": "The numeric ids of the tasks to add."
          }
        }
        """,
        "sprintId",
        "taskIds");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var sprintId = AiToolSchema.GetInt(arguments, "sprintId");

        if (!sprintId.HasValue)
        {
            return AiToolExecution.Failed("A sprintId is required.");
        }

        var taskIds = ReadTaskIds(arguments);

        if (taskIds.Count == 0)
        {
            return AiToolExecution.Failed("At least one taskId is required.");
        }

        var isOverLimit = taskIds.Count > MaximumTasks;

        if (isOverLimit)
        {
            return AiToolExecution.Failed($"No more than {MaximumTasks} tasks can be added in one change.");
        }

        var sprint = await AiSprintLookup.Find(Mediator, sprintId.Value, cancellationToken);

        if (sprint is null)
        {
            return AiToolExecution.Failed($"Sprint {sprintId} is not in this workspace.");
        }

        var isCompleted = sprint.Status == SprintStatus.Completed;

        if (isCompleted)
        {
            return AiToolExecution.Failed($"Sprint “{sprint.Name}” is completed and can no longer be changed.");
        }

        var pendingIds = new List<int>();
        var pendingLabels = new List<string>();

        foreach (var taskId in taskIds)
        {
            var task = await Mediator.Send(new GetTaskQuery(taskId), cancellationToken);

            if (task is null)
            {
                return AiToolExecution.Failed($"Task {taskId} was not found in this workspace.");
            }

            var belongsToProject = task.ProjectId == sprint.ProjectId;

            if (!belongsToProject)
            {
                return AiToolExecution.Failed($"Task {task.SystemId} is not in project {sprint.ProjectName}.");
            }

            var isAlreadyInSprint = task.SprintId == sprint.Id;

            if (isAlreadyInSprint)
            {
                continue;
            }

            pendingIds.Add(task.Id);
            pendingLabels.Add($"{task.SystemId} · {task.Name}");
        }

        if (pendingIds.Count == 0)
        {
            return AiToolExecution.Failed($"Every task given is already in sprint “{sprint.Name}”.");
        }

        var payload = new
        {
            sprintId = sprint.Id,
            taskIds = pendingIds,
        };

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "sprint",
            EntityId = sprint.Id,
            Summary = $"Add {pendingIds.Count} task(s) to sprint “{sprint.Name}”",
            Fields =
            [
                AiChangeFields.Values(
                    "tasks",
                    AiChangeValueKind.Task,
                    [],
                    pendingLabels.Select(label => AiChangeFields.Task(null, null, label))),
            ],
            Payload = JsonSerializer.SerializeToDocument(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed adding {pendingIds.Count} task(s) to sprint “{sprint.Name}”. Nothing has been applied yet — the user must review and apply the change.");
    }

    private static List<int> ReadTaskIds(JsonElement arguments)
    {
        var isObject = arguments.ValueKind == JsonValueKind.Object;

        if (!isObject)
        {
            return [];
        }

        var hasProperty = arguments.TryGetProperty("taskIds", out var value) && value.ValueKind == JsonValueKind.Array;

        if (!hasProperty)
        {
            return [];
        }

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.Number)
            .Select(item => item.GetInt32())
            .Distinct()
            .ToList();
    }
}
