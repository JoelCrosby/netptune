using Netptune.Core.Entities;
using Netptune.Core.Models.Ai;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Core.Services.Ai;

public interface IAiSpendService
{
    Task<AiSpendStatus> GetStatus(Workspace workspace, CancellationToken cancellationToken = default);

    Task<AiSpendViewModel> GetSummary(Workspace workspace, CancellationToken cancellationToken = default);
}
