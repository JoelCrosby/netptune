using Netptune.Core.Entities;
using Netptune.Core.Models.Ai;

namespace Netptune.Core.Services.Ai;

public sealed record AiChangeApplyContext
{
    public required AiProposedChange Change { get; init; }

    public required IReadOnlyDictionary<string, int> ResolvedRefs { get; init; }
}

public interface IAiChangeHandler
{
    string ToolName { get; }

    Task<AiAppliedChangeResult> Apply(AiChangeApplyContext context, CancellationToken cancellationToken);
}
