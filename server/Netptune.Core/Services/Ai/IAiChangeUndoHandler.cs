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

    Task<JsonDocument?> Capture(AiChangeApplyContext context, CancellationToken cancellationToken);

    Task<AiAppliedChangeResult> Revert(AiChangeUndoContext context, CancellationToken cancellationToken);
}
