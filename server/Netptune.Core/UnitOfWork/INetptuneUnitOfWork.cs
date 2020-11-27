using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.UnitOfWork
{
    public interface INetptuneUnitOfWork : IUnitOfWork
    {
        IProjectRepository Projects { get; }

        IWorkspaceRepository Workspaces { get; }

        ITaskRepository Tasks { get; }

        IUserRepository Users { get; }

        IBoardRepository Boards { get; }

        IBoardGroupRepository BoardGroups { get; }

        ITaskInGroupRepository ProjectTasksInGroups { get; }

        IProjectTaskTagRepository ProjectTaskTags { get; }

        ICommentRepository Comments { get; }

        IReactionRepository Reactions { get; }

        ITagRepository Tags { get; }
    }
}
