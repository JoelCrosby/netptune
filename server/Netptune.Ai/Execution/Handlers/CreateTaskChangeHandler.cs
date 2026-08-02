using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Tasks.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class CreateTaskChangeHandler : IAiChangeHandler, IAiChangeUndoHandler
{
    private readonly IMediator Mediator;

    public CreateTaskChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_create_task";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var payload = change.Payload.RootElement;
        var request = new AddProjectTaskRequest
        {
            Name = AiChangePayload.ReadString(payload, "name") ?? string.Empty,
            Description = AiChangePayload.ReadString(payload, "description") ?? string.Empty,
            ProjectId = AiChangePayload.ReadInt(payload, "projectId"),
            StatusId = AiChangePayload.ReadInt(payload, "statusId"),
            AssigneeId = AiChangePayload.ReadString(payload, "assigneeId"),
            SprintId = AiChangePayload.ReadInt(payload, "sprintId")
                ?? AiChangePayload.ResolveReference(context, "sprintRef"),
            BoardGroupId = AiChangePayload.ReadInt(payload, "boardGroupId"),
            Priority = ReadEnum<TaskPriority>(payload, "priority"),
            EstimateType = ReadEnum<EstimateType>(payload, "estimateType"),
            EstimateValue = AiChangePayload.ReadDecimal(payload, "estimateValue"),
            StartDate = AiChangePayload.ReadDate(payload, "startDate"),
            DueDate = AiChangePayload.ReadDate(payload, "dueDate"),
        };

        var response = await Mediator.Send(new CreateTaskCommand(request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The task could not be created.");
        }

        var taskId = response.Payload?.Id;
        var tags = AiChangePayload.ReadStringArray(payload, "tags");
        var hasTags = tags.Count > 0 && taskId.HasValue;

        if (!hasTags)
        {
            return AiChangePayload.Applied(change, taskId);
        }

        var tagRequest = new UpdateProjectTaskRequest { Id = taskId!.Value, Tags = tags };
        var tagResponse = await Mediator.Send(new UpdateTaskCommand(tagRequest), cancellationToken);

        if (!tagResponse.IsSuccess)
        {
            return new AiAppliedChangeResult
            {
                ChangeId = change.Id,
                Status = AiChangeApplyStatus.Failed,
                AppliedEntityId = taskId,
                Error = "The task was created but its tags could not be set.",
            };
        }

        return AiChangePayload.Applied(change, taskId);
    }

    private static TValue? ReadEnum<TValue>(JsonElement payload, string name)
        where TValue : struct, Enum
    {
        var raw = AiChangePayload.ReadString(payload, name);
        var isParsed = Enum.TryParse<TValue>(raw, true, out var value);

        return isParsed ? value : null;
    }

    public IReadOnlySet<string> UndoPermissions { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        NetptunePermissions.Tasks.Delete,
    };

    /// <summary>Nothing existed before, so the undo only needs the created task.</summary>
    public Task<JsonDocument?> Capture(AiChangeApplyContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult<JsonDocument?>(null);
    }

    public async Task<AiAppliedChangeResult> Revert(
        AiChangeUndoContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var taskId = change.AppliedEntityId;

        if (!taskId.HasValue)
        {
            return AiChangeUndoResult.Failure(change, "The created task could not be resolved.");
        }

        var response = await Mediator.Send(new DeleteTaskCommand(taskId.Value), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangeUndoResult.Failure(change, response.Message ?? "The created task could not be deleted.");
        }

        return AiChangeUndoResult.Undone(change, taskId);
    }
}
