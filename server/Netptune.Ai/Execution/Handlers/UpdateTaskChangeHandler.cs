using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Tasks.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class UpdateTaskChangeHandler : IAiChangeHandler, IAiChangeUndoHandler
{
    private readonly IMediator Mediator;

    public UpdateTaskChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_update_task";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var taskId = AiChangePayload.ResolveTaskId(context);

        if (!taskId.HasValue)
        {
            return AiChangePayload.Failure(change, "The task this change refers to could not be resolved.");
        }

        var payload = change.Payload.RootElement;
        var request = new UpdateProjectTaskRequest
        {
            Id = taskId.Value,
            Name = AiChangePayload.ReadString(payload, "name"),
            Description = AiChangePayload.ReadString(payload, "description"),
            StatusId = AiChangePayload.ReadInt(payload, "statusId"),
            Priority = ReadPriority(payload),
            EstimateType = ReadEstimateType(payload),
            EstimateValue = AiChangePayload.ReadDecimal(payload, "estimateValue"),
        };

        var cleared = AiChangePayload.ReadStringArray(payload, "clear");

        ApplyDate(payload, "startDate", cleared, date => request.StartDate = date);
        ApplyDate(payload, "dueDate", cleared, date => request.DueDate = date);

        var response = await Mediator.Send(new UpdateTaskCommand(request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The task could not be updated.");
        }

        return AiChangePayload.Applied(change, taskId);
    }

    private static void ApplyDate(
        JsonElement payload,
        string name,
        List<string> cleared,
        Action<DateOnly?> apply)
    {
        var isCleared = cleared.Contains(name, StringComparer.Ordinal);

        if (isCleared)
        {
            apply(null);

            return;
        }

        var date = AiChangePayload.ReadDate(payload, name);

        if (!date.HasValue)
        {
            return;
        }

        apply(date);
    }

    private static TaskPriority? ReadPriority(JsonElement payload)
    {
        var raw = AiChangePayload.ReadString(payload, "priority");
        var isParsed = Enum.TryParse<TaskPriority>(raw, true, out var priority);

        return isParsed ? priority : null;
    }

    private static EstimateType? ReadEstimateType(JsonElement payload)
    {
        var raw = AiChangePayload.ReadString(payload, "estimateType");
        var isParsed = Enum.TryParse<EstimateType>(raw, true, out var type);

        return isParsed ? type : null;
    }

    public IReadOnlySet<string> UndoPermissions { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        NetptunePermissions.Tasks.Update,
    };

    public Task<JsonDocument?> Capture(AiChangeApplyContext context, CancellationToken cancellationToken)
    {
        return AiTaskUndo.Capture(Mediator, AiChangePayload.ResolveTaskId(context), cancellationToken);
    }

    public Task<AiAppliedChangeResult> Revert(AiChangeUndoContext context, CancellationToken cancellationToken)
    {
        return AiTaskUndo.Restore(Mediator, context, cancellationToken);
    }
}
