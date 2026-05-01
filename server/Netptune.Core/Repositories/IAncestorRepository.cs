using Netptune.Core.Models.Activity;

namespace Netptune.Core.Repositories;

public interface IAncestorRepository
{
    Task<ActivityAncestors> GetProjectTaskAncestors(int taskId, CancellationToken cancellationToken = default);

    Task<ActivityAncestors> GetBoardGroupAncestors(int boardGroupId, CancellationToken cancellationToken = default);

    Task<ActivityAncestors> GetBoardAncestors(int boardId, CancellationToken cancellationToken = default);

    Task<ActivityAncestors> GetProjectAncestors(int projectId, CancellationToken cancellationToken = default);
}
