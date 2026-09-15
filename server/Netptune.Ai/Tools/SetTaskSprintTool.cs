using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.Ai.Tools;

public sealed class SetTaskSprintTool : IAiTool
{
    public const string MoveChange = "propose_move_task_to_sprint";
    public const string AddChange = "propose_add_tasks_to_sprint";
    public const string RemoveChange = "propose_remove_task_from_sprint";

    private const int MaximumTasks = 50;

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public SetTaskSprintTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_set_task_sprint";

    public string Description =>
        "Propose putting tasks into a sprint, or set backlog to take them out of the sprint they are in. "
        + "Tasks must belong to the sprint's project. Find sprint ids with list_sprints, "
        + "or pass handles of a sprint or tasks proposed earlier in this change set.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Sprints.ManageTasks };

    public IReadOnlyList<string> ProposedChanges { get; } = [MoveChange, AddChange, RemoveChange];

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "taskIds": { "type": "array", "items": { "type": "integer" }, "description": "Ids of the tasks to move." },
          "taskRefs": {
            "type": "array",
            "items": { "type": "string" },
            "description": "Handles of tasks proposed earlier in this change set."
          },
          "sprintId": { "type": "integer", "description": "The sprint to move the tasks into." },
          "sprintRef": { "type": "string", "description": "Handle of a sprint proposed earlier in this change set, instead of sprintId." },
          "backlog": { "type": "boolean", "description": "True to take the tasks out of their sprint instead." }
        }
        """);

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var taskIds = AiToolSchema.GetIntArray(arguments, "taskIds").Distinct().ToList();
        var taskRefs = AiToolSchema.GetStringArray(arguments, "taskRefs").Distinct(StringComparer.Ordinal).ToList();
        var taskCount = taskIds.Count + taskRefs.Count;

        if (taskCount == 0)
        {
            return AiToolExecution.Failed(
                "At least one taskId, or a taskRef for a task proposed in this change set, is required.");
        }

        var isOverLimit = taskCount > MaximumTasks;

        if (isOverLimit)
        {
            return AiToolExecution.Failed($"No more than {MaximumTasks} tasks can be moved in one change.");
        }

        var toBacklog = AiToolSchema.GetBool(arguments, "backlog") == true;
        var hasSprintId = AiToolSchema.GetInt(arguments, "sprintId").HasValue;
        var hasSprintRef = AiPendingReference.Read(arguments, "sprintRef") is not null;
        var hasSprint = hasSprintId || hasSprintRef;

        if (toBacklog && hasSprint)
        {
            return AiToolExecution.Failed("Pass a sprint to move the tasks into, or backlog true, not both.");
        }

        if (toBacklog)
        {
            return await ProposeRemoval(taskIds, taskRefs, cancellationToken);
        }

        if (!hasSprint)
        {
            return AiToolExecution.Failed(
                "A sprintId or sprintRef is required, or backlog true to take the tasks out of their sprint.");
        }

        return await ProposeMove(arguments, taskIds, taskRefs, cancellationToken);
    }

    private async Task<AiToolExecution> ProposeRemoval(
        List<int> taskIds,
        List<string> taskRefs,
        CancellationToken cancellationToken)
    {
        var hasPendingTasks = taskRefs.Count > 0;

        if (hasPendingTasks)
        {
            return AiToolExecution.Failed("Tasks proposed in this change set are not in a sprint to be taken out of.");
        }

        var drafts = new List<AiChangeDraft>();

        foreach (var taskId in taskIds)
        {
            var task = await Mediator.Send(new GetTaskQuery(taskId), cancellationToken);

            if (task is null)
            {
                return AiToolExecution.Failed($"Task {taskId} was not found in this workspace.");
            }

            if (!task.SprintId.HasValue)
            {
                return AiToolExecution.Failed($"Task {task.SystemId} is not in a sprint.");
            }

            drafts.Add(CreateRemoval(task));
        }

        return Propose(drafts, $"taking {drafts.Count} task(s) out of their sprint");
    }

    private async Task<AiToolExecution> ProposeMove(
        JsonElement arguments,
        List<int> taskIds,
        List<string> taskRefs,
        CancellationToken cancellationToken)
    {
        var target = await AiSprintTargetLookup.Resolve(Mediator, ChangeSet, arguments, cancellationToken);

        if (target.Error is not null)
        {
            return AiToolExecution.Failed(target.Error);
        }

        var sprint = target.Sprint!;
        var drafts = new List<AiChangeDraft>();

        foreach (var taskRef in taskRefs)
        {
            var pendingTask = AiPendingReference.Find(ChangeSet, taskRef, "task");

            if (pendingTask is null)
            {
                return AiToolExecution.Failed(AiPendingReference.Missing(taskRef, "task"));
            }

            var taskName = AiPendingReference.ProposedName(pendingTask);

            drafts.Add(CreateMove(sprint, null, taskRef, taskName));
        }

        var tasks = new List<TaskViewModel>();

        foreach (var taskId in taskIds)
        {
            var task = await Mediator.Send(new GetTaskQuery(taskId), cancellationToken);

            if (task is null)
            {
                return AiToolExecution.Failed($"Task {taskId} was not found in this workspace.");
            }

            var mismatch = AiSprintTargetLookup.FindProjectMismatch(sprint, task);

            if (mismatch is not null)
            {
                return AiToolExecution.Failed(mismatch);
            }

            var isAlreadyInSprint = sprint.Id.HasValue && task.SprintId == sprint.Id;

            if (!isAlreadyInSprint)
            {
                tasks.Add(task);
            }
        }

        var hasExistingTasks = taskIds.Count > 0;
        var isEveryTaskAlreadyInSprint = hasExistingTasks && tasks.Count == 0 && drafts.Count == 0;

        if (isEveryTaskAlreadyInSprint)
        {
            return AiToolExecution.Failed($"Every task given is already in sprint “{sprint.Name}”.");
        }

        if (tasks.Count == 1)
        {
            drafts.Add(CreateMove(sprint, tasks[0], null, tasks[0].Name));
        }
        else if (tasks.Count > 1)
        {
            drafts.Add(CreateAddition(sprint, tasks));
        }

        var movedCount = taskRefs.Count + tasks.Count;

        return Propose(drafts, $"moving {movedCount} task(s) into sprint “{sprint.Name}”");
    }

    private AiToolExecution Propose(List<AiChangeDraft> drafts, string description)
    {
        foreach (var draft in drafts)
        {
            ChangeSet.Add(draft);
        }

        return AiToolExecution.Success(
            $"Proposed {description}. Nothing has been applied yet — the user must review and apply the change.");
    }

    private static AiChangeDraft CreateMove(AiSprintTarget sprint, TaskViewModel? task, string? taskRef, string taskName)
    {
        var payload = new
        {
            taskId = task?.Id,
            taskRef,
            sprintId = sprint.Id,
            sprintRef = sprint.RefKey,
        };

        return new AiChangeDraft
        {
            ToolName = MoveChange,
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
            Payload = JsonSerializer.SerializeToDocument(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
        };
    }

    private static AiChangeDraft CreateAddition(AiSprintTarget sprint, List<TaskViewModel> tasks)
    {
        var payload = new
        {
            sprintId = sprint.Id,
            sprintRef = sprint.RefKey,
            taskIds = tasks.Select(task => task.Id).ToList(),
        };

        var labels = tasks.Select(task => AiChangeFields.Task(null, null, $"{task.SystemId} · {task.Name}"));

        return new AiChangeDraft
        {
            ToolName = AddChange,
            EntityType = "sprint",
            EntityId = sprint.Id,
            Summary = $"Add {tasks.Count} task(s) to sprint “{sprint.Name}”",
            Fields = [AiChangeFields.Values("tasks", AiChangeValueKind.Task, [], labels)],
            Payload = JsonSerializer.SerializeToDocument(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
        };
    }

    private static AiChangeDraft CreateRemoval(TaskViewModel task)
    {
        var payload = new
        {
            taskId = task.Id,
            sprintId = task.SprintId!.Value,
        };

        return new AiChangeDraft
        {
            ToolName = RemoveChange,
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
        };
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
