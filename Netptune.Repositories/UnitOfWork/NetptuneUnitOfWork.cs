using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.UnitOfWork;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories.UnitOfWork
{
    public class NetptuneUnitOfWork : UnitOfWork<DataContext, IDbConnectionFactory>, INetptuneUnitOfWork
    {
        public IProjectRepository Projects { get; }
        public IWorkspaceRepository Workspaces { get; }
        public ITaskRepository Tasks { get; }
        public IUserRepository Users { get; }

        public NetptuneUnitOfWork(DataContext context, IDbConnectionFactory connectionFactory) : base(context, connectionFactory)
        {
            Projects = new ProjectRepository(context, connectionFactory);
            Tasks = new TaskRepository(context, connectionFactory);
            Users = new UserRepository(context, connectionFactory);
            Workspaces = new WorkspaceRepository(context, connectionFactory);
        }
    }
}
