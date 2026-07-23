using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Flags;

namespace Netptune.Core.Repositories;

public interface IFlagRepository : IWorkspaceEntityRepository<Flag, int>
{
    Task<List<Flag>> GetExistingAutomationTaskFlags(IReadOnlyCollection<int> ruleIds, IReadOnlyCollection<int> taskIds, CancellationToken cancellationToken = default);

    Task<List<TaskFlagViewModel>> GetActiveTaskFlags(int taskId, int workspaceId, CancellationToken cancellationToken = default);

    Task<Flag?> GetTaskFlagForUpdate(int flagId, int taskId, int workspaceId, CancellationToken cancellationToken = default);
}
