using Netptune.Core.Models.Activity;

namespace Netptune.Core.Repositories;

public interface IAncestorRepository
{
    Task<ActivityAncestors> GetProjectTaskAncestors(int taskId);

    Task<ActivityAncestors> GetBoardGroupAncestors(int boardGroupId);

    Task<ActivityAncestors> GetBoardAncestors(int boardId);

    Task<ActivityAncestors> GetProjectAncestors(int projectId);
}
