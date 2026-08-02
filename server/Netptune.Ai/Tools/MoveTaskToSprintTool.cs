using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Sprints.Queries;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.Ai.Tools;

public sealed class MoveTaskToSprintTool : IAiTool
{
    private const int SprintLookupTake = 200;

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public MoveTaskToSprintTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_move_task_to_sprint";

    public string Description =>
        "Propose moving a task into a sprint. Use list_sprints to find sprint ids first. Nothing is applied until the user approves it.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Sprints.ManageTasks };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "taskId": { "type": "integer", "description": "The id of the task to move." },
          "sprintId": { "type": "integer", "description": "The sprint to move the task into." }
        }
        """,
        "taskId",
        "sprintId");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var taskId = AiToolSchema.GetInt(arguments, "taskId");
        var sprintId = AiToolSchema.GetInt(arguments, "sprintId");

        if (!taskId.HasValue || !sprintId.HasValue)
        {
            return AiToolExecution.Failed("A taskId and sprintId are required.");
        }

        var task = await Mediator.Send(new GetTaskQuery(taskId.Value), cancellationToken);

        if (task is null)
        {
            return AiToolExecution.Failed($"Task {taskId} was not found in this workspace.");
        }

        var sprints = await Mediator.Send(new GetSprintsQuery(null, [], SprintLookupTake), cancellationToken);
        var sprint = sprints.FirstOrDefault(item => item.Id == sprintId.Value);

        if (sprint is null)
        {
            return AiToolExecution.Failed($"Sprint {sprintId} is not in this workspace.");
        }

        var belongsToProject = sprint.ProjectId == task.ProjectId;

        if (!belongsToProject)
        {
            return AiToolExecution.Failed($"Sprint “{sprint.Name}” belongs to a different project than task {task.Id}.");
        }

        var isAlreadyInSprint = task.SprintId == sprint.Id;

        if (isAlreadyInSprint)
        {
            return AiToolExecution.Failed($"Task {task.Id} is already in sprint “{sprint.Name}”.");
        }

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "task",
            EntityId = task.Id,
            Summary = $"Move “{task.Name}” into sprint “{sprint.Name}”",
            Fields =
            [
                AiChangeFields.Values(
                    "sprint",
                    AiChangeValueKind.Sprint,
                    task.SprintId.HasValue ? [AiChangeFields.Sprint(task.SprintId, task.SprintName!)] : [],
                    [AiChangeFields.Sprint(sprint.Id, sprint.Name)]),
            ],
            Payload = JsonDocument.Parse(arguments.GetRawText()),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed moving task {task.Id} into sprint “{sprint.Name}”. Nothing has been applied yet — the user must review and apply the change.");
    }
}
