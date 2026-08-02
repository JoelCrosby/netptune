using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Tasks.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class SetTaskTagsChangeHandler : IAiChangeHandler, IAiChangeUndoHandler
{
    private readonly IMediator Mediator;

    public SetTaskTagsChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_set_task_tags";

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

        var request = new UpdateProjectTaskRequest
        {
            Id = taskId.Value,
            Tags = AiChangePayload.ReadStringArray(change.Payload.RootElement, "tags"),
        };

        var response = await Mediator.Send(new UpdateTaskCommand(request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The task tags could not be updated.");
        }

        return AiChangePayload.Applied(change, taskId);
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
