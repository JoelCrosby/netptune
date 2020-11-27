using Netptune.Core.Relationships;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories
{
    public class ProjectTaskTagRepository : Repository<DataContext, ProjectTaskTag, int>, IProjectTaskTagRepository
    {
        public ProjectTaskTagRepository(DataContext context, IDbConnectionFactory connectionFactory)
            : base(context, connectionFactory)
        {
        }
    }
}
