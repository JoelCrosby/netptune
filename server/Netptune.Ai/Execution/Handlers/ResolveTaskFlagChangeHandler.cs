using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Flags.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class ResolveTaskFlagChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public ResolveTaskFlagChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_resolve_task_flag";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var payload = change.Payload.RootElement;
        var taskId = AiChangePayload.ResolveTaskId(context);
        var flagId = AiChangePayload.ReadInt(payload, "flagId");

        if (!taskId.HasValue || !flagId.HasValue)
        {
            return AiChangePayload.Failure(change, "The task or flag this change refers to could not be resolved.");
        }

        var raw = AiChangePayload.ReadString(payload, "resolution");
        var isParsed = Enum.TryParse<FlagResolutionType>(raw, true, out var resolution);

        if (!isParsed)
        {
            return AiChangePayload.Failure(change, "The flag resolution is missing from this change.");
        }

        var request = new ResolveTaskFlagRequest { Resolution = resolution };
        var response = await Mediator.Send(
            new ResolveTaskFlagCommand(taskId.Value, flagId.Value, request),
            cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The flag could not be cleared.");
        }

        return AiChangePayload.Applied(change, taskId);
    }
}
