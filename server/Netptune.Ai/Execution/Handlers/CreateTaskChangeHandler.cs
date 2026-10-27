using System.Text.Json;

using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Localization;
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
        var description = AiChangePayload.ReadString(payload, "description");
        var request = new AddProjectTaskRequest
        {
            Name = AiChangePayload.ReadString(payload, "name") ?? string.Empty,
            Description = description ?? string.Empty,
            ProjectId = AiChangePayload.ReadInt(payload, "projectId")
                ?? AiChangePayload.ResolveReference(context, "projectRef"),
            StatusId = AiChangePayload.ReadInt(payload, "statusId"),
            AssigneeId = AiChangePayload.ReadString(payload, "assigneeId"),
            SprintId = AiChangePayload.ReadInt(payload, "sprintId")
                ?? AiChangePayload.ResolveReference(context, "sprintRef"),
            BoardGroupId = AiChangePayload.ReadInt(payload, "boardGroupId")
                ?? AiChangePayload.ResolveReference(context, "boardGroupRef"),
            Priority = ReadEnum<TaskPriority>(payload, "priority"),
            EstimateType = ReadEnum<EstimateType>(payload, "estimateType"),
            EstimateValue = AiChangePayload.ReadDecimal(payload, "estimateValue"),
            StartDate = AiChangePayload.ReadDate(payload, "startDate"),
            DueDate = AiChangePayload.ReadDate(payload, "dueDate"),
            Tags = AiChangePayload.ReadStringArray(payload, "tags"),
        };

        var response = await Mediator.Send(new CreateTaskCommand(request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? AppText.AiChangeErrorTaskCouldNotCreated);
        }

        return AiChangePayload.Applied(change, response.Payload?.Id);
    }

    private static TValue? ReadEnum<TValue>(JsonElement payload, string name)
        where TValue : struct, Enum
    {
        var raw = AiChangePayload.ReadString(payload, name);
        var isParsed = AiToolSchema.TryParseEnum<TValue>(raw, out var value);

        return isParsed ? value : null;
    }

    public IReadOnlySet<string> UndoPermissions { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        NetptunePermissions.Tasks.Delete,
    };

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
            return AiChangePayload.Failure(change, AppText.AiChangeErrorCreatedTaskCouldNotResolved);
        }

        var response = await Mediator.Send(new DeleteTaskCommand(taskId.Value), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(
                change,
                response.Message ?? AppText.AiChangeErrorCreatedTaskCouldNotDeleted);
        }

        return AiChangePayload.Applied(change, taskId);
    }
}
