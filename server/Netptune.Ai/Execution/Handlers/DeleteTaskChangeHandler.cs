using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Localization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Tasks.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class DeleteTaskChangeHandler : IAiChangeHandler, IAiChangeUndoHandler
{
    private readonly IMediator Mediator;

    public DeleteTaskChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_delete_task";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var taskId = AiChangePayload.ResolveTaskId(context);

        if (!taskId.HasValue)
        {
            return AiChangePayload.Failure(change, AppText.AiChangeErrorTaskChangeRefersCouldNotResolved);
        }

        var response = await Mediator.Send(new DeleteTaskCommand(taskId.Value), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? AppText.AiChangeErrorTaskCouldNotDeleted);
        }

        return AiChangePayload.Applied(change, taskId);
    }

    public IReadOnlySet<string> UndoPermissions { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        NetptunePermissions.Tasks.Restore,
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
        var taskId = change.AppliedEntityId ?? change.EntityId;

        if (!taskId.HasValue)
        {
            return AiChangePayload.Failure(change, AppText.AiChangeErrorDeletedTaskCouldNotResolved);
        }

        var response = await Mediator.Send(new RestoreTasksCommand([taskId.Value]), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? AppText.AiChangeErrorTaskCouldNotRestored);
        }

        return AiChangePayload.Applied(change, taskId);
    }
}
