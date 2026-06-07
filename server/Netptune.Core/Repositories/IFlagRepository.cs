using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface IFlagRepository : IWorkspaceEntityRepository<Flag, int>
{
    Task<List<Flag>> GetExistingAutomationTaskFlags(
        IReadOnlyCollection<int> ruleIds,
        IReadOnlyCollection<int> taskIds,
        CancellationToken cancellationToken = default);
}
