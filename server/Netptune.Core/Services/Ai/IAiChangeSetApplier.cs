using Netptune.Core.Models.Ai;

namespace Netptune.Core.Services.Ai;

public interface IAiChangeSetApplier
{
    Task<AiApplyResult?> Apply(Guid changeSetId, ApplyAiChangeSetRequest request, CancellationToken cancellationToken);
}
