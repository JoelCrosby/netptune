using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;

namespace Netptune.Core.Services.Ai;

public interface IAiModelCatalog
{
    Task<List<AiModelOption>> List(IReadOnlyList<AiResolvedCredential> credentials, CancellationToken cancellationToken);

    Task<IReadOnlyList<AiEffort>> EffortsFor(AiResolvedCredential credential, string model, CancellationToken cancellationToken);
}
