using System.Text.Json;

using Netptune.Core.Entities;
using Netptune.Core.Models.Ai;

namespace Netptune.Core.Services.Ai;

public sealed record AiChangeUndoContext
{
    public required AiProposedChange Change { get; init; }
}

public interface IAiChangeUndoHandler
{
    IReadOnlySet<string> UndoPermissions { get; }

    IReadOnlySet<string> GetUndoPermissions(JsonElement payload) => UndoPermissions;

    Task<JsonDocument?> Capture(AiChangeApplyContext context, CancellationToken cancellationToken);

    // Runs once the change has landed, so the undo can tell what the change itself did from what other
    // people have done since.
    Task<JsonDocument?> CaptureApplied(AiChangeApplyContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(context.Change.UndoPayload);
    }

    Task<AiAppliedChangeResult> Revert(AiChangeUndoContext context, CancellationToken cancellationToken);
}
