using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.Ai.Tools;

public sealed class MoveTaskToSprintTool : IAiTool
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public MoveTaskToSprintTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_move_task_to_sprint";

    public string Description =>
        "Propose moving a task into a sprint. Use list_sprints to find sprint ids first, or pass the handle of a task "
        + "or sprint proposed earlier in this change set. Nothing is applied until the user approves it.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Sprints.ManageTasks };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "taskId": { "type": "integer", "description": "The id of the task to move." },
          "taskRef": { "type": "string", "description": "Handle of a task proposed earlier in this change set, instead of taskId." },
          "sprintId": { "type": "integer", "description": "The sprint to move the task into." },
          "sprintRef": { "type": "string", "description": "Handle of a sprint proposed earlier in this change set, instead of sprintId." }
        }
        """);

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var taskId = AiToolSchema.GetInt(arguments, "taskId");
        var taskRef = AiPendingReference.Read(arguments, "taskRef");
        var hasTask = taskId.HasValue || taskRef is not null;

        if (!hasTask)
        {
            return AiToolExecution.Failed("A taskId is required, or a taskRef for a task proposed in this change set.");
        }

        var pendingTask = taskRef is null ? null : AiPendingReference.Find(ChangeSet, taskRef, "task");

        if (taskRef is not null && pendingTask is null)
        {
            return AiToolExecution.Failed(AiPendingReference.Missing(taskRef, "task"));
        }

        var target = await AiSprintTargetLookup.Resolve(Mediator, ChangeSet, arguments, cancellationToken);

        if (target.Error is not null)
        {
            return AiToolExecution.Failed(target.Error);
        }

        var sprint = target.Sprint!;
        var task = taskId.HasValue ? await Mediator.Send(new GetTaskQuery(taskId.Value), cancellationToken) : null;

        if (taskId.HasValue && task is null)
        {
            return AiToolExecution.Failed($"Task {taskId} was not found in this workspace.");
        }

        var taskError = task is null ? null : ValidateTask(task, sprint);

        if (taskError is not null)
        {
            return AiToolExecution.Failed(taskError);
        }

        var taskName = task?.Name ?? AiPendingReference.ProposedName(pendingTask!);

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "task",
            EntityId = task?.Id,
            Summary = $"Move “{taskName}” into sprint “{sprint.Name}”",
            Fields =
            [
                AiChangeFields.Values(
                    "sprint",
                    AiChangeValueKind.Sprint,
                    ReadCurrentSprint(task),
                    [AiChangeFields.Sprint(sprint.Id, sprint.Name)]),
            ],
            Payload = JsonDocument.Parse(arguments.GetRawText()),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed moving “{taskName}” into sprint “{sprint.Name}”. Nothing has been applied yet — the user must review and apply the change.");
    }

    private static string? ValidateTask(TaskViewModel task, AiSprintTarget sprint)
    {
        var mismatch = AiSprintTargetLookup.FindProjectMismatch(sprint, task);

        if (mismatch is not null)
        {
            return mismatch;
        }

        var isAlreadyInSprint = sprint.Id.HasValue && task.SprintId == sprint.Id;

        if (isAlreadyInSprint)
        {
            return $"Task {task.Id} is already in sprint “{sprint.Name}”.";
        }

        return null;
    }

    private static List<AiChangeValue> ReadCurrentSprint(TaskViewModel? task)
    {
        var hasSprint = task?.SprintId.HasValue == true;

        if (!hasSprint)
        {
            return [];
        }

        return [AiChangeFields.Sprint(task!.SprintId, task.SprintName!)];
    }
}
