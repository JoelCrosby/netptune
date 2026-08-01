using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.BoardGroups.Queries;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.Ai.Tools;

public sealed class MoveTaskToBoardGroupTool : IAiTool
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public MoveTaskToBoardGroupTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_move_task_to_board_group";

    public string Description =>
        "Propose moving a task into a board group, the column it sits in on a board. "
        + "The move is not applied until the user reviews and applies the change.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Tasks.Move };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "taskId": { "type": "integer", "description": "The id of the task to move." },
          "boardGroupId": { "type": "integer", "description": "The target board group id, from list_board_groups." }
        }
        """,
        "taskId",
        "boardGroupId");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var taskId = AiToolSchema.GetInt(arguments, "taskId");
        var boardGroupId = AiToolSchema.GetInt(arguments, "boardGroupId");

        if (!taskId.HasValue || !boardGroupId.HasValue)
        {
            return AiToolExecution.Failed("A taskId and boardGroupId are required.");
        }

        var task = await Mediator.Send(new GetTaskQuery(taskId.Value), cancellationToken);

        if (task is null)
        {
            return AiToolExecution.Failed($"Task {taskId} was not found in this workspace.");
        }

        var options = await Mediator.Send(new GetBoardGroupOptionsQuery(), cancellationToken);
        var group = options.FirstOrDefault(option => option.Id == boardGroupId.Value);

        if (group is null)
        {
            return AiToolExecution.Failed($"Board group {boardGroupId} is not in this workspace.");
        }

        var payload = new
        {
            taskId = task.Id,
            boardGroupId = group.Id,
            boardIdentifier = group.BoardIdentifier,
        };

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "task",
            EntityId = task.Id,
            Summary = $"Move “{task.Name}” to {group.Name} on {group.BoardName}",
            Fields =
            [
                new AiChangeField { Name = "boardGroup", After = $"{group.BoardName} · {group.Name}" },
            ],
            Payload = JsonSerializer.SerializeToDocument(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed moving task {task.Id} to {group.Name}. Nothing has been applied yet — the user must review and apply the change.");
    }
}
