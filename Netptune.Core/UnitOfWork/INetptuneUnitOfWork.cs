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
    }
}
