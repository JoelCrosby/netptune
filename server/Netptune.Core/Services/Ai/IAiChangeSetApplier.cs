using Netptune.Core.Models.Ai;

namespace Netptune.Core.Services.Ai;

public interface IAiChangeSetApplier
{
    Task<AiApplyResult?> Apply(Guid changeSetId, ApplyAiChangeSetRequest request, CancellationToken cancellationToken);

    Task<AiApplyResult?> Undo(Guid changeSetId, CancellationToken cancellationToken);

    Task<AiApplyResult?> RetryFailed(Guid changeSetId, CancellationToken cancellationToken);
}
